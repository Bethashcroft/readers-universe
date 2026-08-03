using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Import;

public class ImportOutcome
{
    public int Added { get; set; }
    public int AlreadyOnShelves { get; set; }
    public int ReviewsAdded { get; set; }
    public int NewToCatalogue { get; set; }
    public Dictionary<string, int> ByShelf { get; set; } = [];
}

public class LibraryImportService(AppDbContext context)
{
    private readonly AppDbContext _context = context;

    public async Task<ImportOutcome> ApplyAsync(
        List<ImportedBook> imported,
        string userId,
        bool commit
    )
    {
        var outcome = new ImportOutcome();

        var isbns = imported
            .Select(b => Book.CleanIsbn(b.Isbn))
            .Where(s => s.Length > 0)
            .Distinct()
            .ToList();

        var matchKeys = imported
            .Select(b => Book.BuildMatchKey(b.Title, b.Author))
            .Distinct()
            .ToList();

        var existingBooks = await _context
            .Books.Where(b =>
                (b.Isbn != string.Empty && isbns.Contains(b.Isbn)) || matchKeys.Contains(b.MatchKey)
            )
            .ToListAsync();

        var byIsbn = existingBooks
            .Where(b => b.Isbn.Length > 0)
            .GroupBy(b => b.Isbn)
            .ToDictionary(g => g.Key, g => g.First());

        var byMatchKey = existingBooks
            .GroupBy(b => b.MatchKey)
            .ToDictionary(g => g.Key, g => g.First());

        var myBookIds = (
            await _context.LibraryEntries.Where(e => e.UserId == userId)
                .Select(e => e.BookId)
                .ToListAsync()
        ).ToHashSet();

        var myReviewedBookIds = (
            await _context.Reviews.Where(r => r.UserId == userId).Select(r => r.BookId).ToListAsync()
        ).ToHashSet();

        var handled = new HashSet<Book>();

        foreach (var source in imported)
        {
            var isbn = Book.CleanIsbn(source.Isbn);
            var matchKey = Book.BuildMatchKey(source.Title, source.Author);

            Book? book = null;

            if (isbn.Length > 0)
            {
                byIsbn.TryGetValue(isbn, out book);
            }

            if (book == null)
            {
                byMatchKey.TryGetValue(matchKey, out book);
            }

            if (book == null)
            {
                book = new Book
                {
                    Title = source.Title,
                    Author = source.Author,
                    Isbn = isbn,
                    MatchKey = matchKey,
                    CoverUrl = CoverUrlFor(source.Title, isbn),
                };

                _context.Books.Add(book);
                byMatchKey[matchKey] = book;

                if (isbn.Length > 0)
                {
                    byIsbn[isbn] = book;
                }

                outcome.NewToCatalogue++;
            }
            else
            {
                if (book.Isbn.Length == 0 && isbn.Length > 0)
                {
                    book.Isbn = isbn;
                }

                if (book.CoverUrl.Contains("placehold.co") && isbn.Length > 0)
                {
                    book.CoverUrl = CoverUrlFor(source.Title, isbn);
                }
            }

            var alreadyMine = (book.Id != 0 && myBookIds.Contains(book.Id)) || !handled.Add(book);

            if (alreadyMine)
            {
                outcome.AlreadyOnShelves++;
                continue;
            }

            _context.LibraryEntries.Add(
                new LibraryEntry
                {
                    Book = book,
                    UserId = userId,
                    Shelf = source.Shelf,
                    Offer = BookOffer.None,
                    AddedDate = source.AddedDate ?? DateTime.UtcNow,
                }
            );

            outcome.Added++;
            outcome.ByShelf[source.Shelf] = outcome.ByShelf.GetValueOrDefault(source.Shelf) + 1;

            var hasReview = source.Rating.HasValue || source.ReviewText.Length > 0;

            if (hasReview && (book.Id == 0 || !myReviewedBookIds.Contains(book.Id)))
            {
                _context.Reviews.Add(
                    new Review
                    {
                        Book = book,
                        UserId = userId,
                        Rating = source.Rating,
                        Text = source.ReviewText,
                        ContainsSpoiler = source.ContainsSpoiler,
                        Date = source.ReadDate ?? source.AddedDate ?? DateTime.UtcNow,
                    }
                );

                outcome.ReviewsAdded++;
            }
        }

        if (commit)
        {
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ChangeTracker.Clear();
        }

        return outcome;
    }

    private static string CoverUrlFor(string title, string isbn) =>
        isbn.Length > 0
            ? $"https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg?default=false"
            : $"https://placehold.co/200x300/1a1430/a9a3cc?text={Uri.EscapeDataString(title)}";
}
