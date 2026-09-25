using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/year-in-books")]
[Authorize]
public class YearInBooksController : ControllerBase
{
    private readonly AppDbContext _context;

    public YearInBooksController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{year}")]
    public async Task<IActionResult> GetYear(int year)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var finishes = await _context
            .ReadingSessions.Where(r => r.LibraryEntry.UserId == me && r.FinishedDate.Year == year)
            .OrderBy(r => r.FinishedDate)
            .ThenBy(r => r.Id)
            .Select(r => new
            {
                r.LibraryEntryId,
                r.FinishedDate,
                r.LibraryEntry.BookId,
                r.LibraryEntry.Book.Title,
                r.LibraryEntry.Book.Author,
                r.LibraryEntry.Book.CoverUrl,
                r.LibraryEntry.PageCount,
                r.LibraryEntry.Format,
            })
            .ToListAsync();

        var bookIds = finishes.Select(f => f.BookId).Distinct().ToList();
        var entryIds = finishes.Select(f => f.LibraryEntryId).Distinct().ToList();

        var ratings = await _context
            .Reviews.Where(r => r.UserId == me && bookIds.Contains(r.BookId) && r.Rating != null)
            .ToDictionaryAsync(r => r.BookId, r => r.Rating!.Value);

        var earlierFinishes = await _context
            .ReadingSessions.Where(r => entryIds.Contains(r.LibraryEntryId))
            .Select(r => new { r.LibraryEntryId, r.FinishedDate })
            .ToListAsync();

        var target = await _context
            .ReadingGoals.Where(g => g.UserId == me && g.Year == year)
            .Select(g => (int?)g.Target)
            .FirstOrDefaultAsync();

        var books = finishes
            .Select(f => new YearBookResponse
            {
                BookId = f.BookId,
                Title = f.Title,
                Author = f.Author,
                CoverUrl = f.CoverUrl,
                PageCount = f.PageCount,
                FinishedDate = f.FinishedDate,
                Rating = ratings.TryGetValue(f.BookId, out var rating) ? rating : null,
            })
            .ToList();

        var withPages = books.Where(b => b.PageCount != null).ToList();
        var distinctBooks = books.DistinctBy(b => b.BookId).ToList();
        var ratedBooks = distinctBooks.Where(b => b.Rating != null).ToList();

        var topAuthor = distinctBooks
            .Where(b => b.Author.Length > 0)
            .GroupBy(b => b.Author)
            .Select(g => new AuthorCountResponse { Name = g.Key, Books = g.Count() })
            .OrderByDescending(a => a.Books)
            .ThenBy(a => a.Name)
            .FirstOrDefault(a => a.Books >= 2);

        var byLength = withPages.DistinctBy(b => b.BookId).OrderBy(b => b.PageCount).ToList();

        return Ok(
            new YearInBooksResponse
            {
                Year = year,
                Target = target,
                BooksRead = books.Count,
                PagesRead = withPages.Sum(b => b.PageCount!.Value),
                BooksWithoutPageCount = books.Count - withPages.Count,
                AverageRating =
                    ratedBooks.Count > 0
                        ? Math.Round(ratedBooks.Average(b => b.Rating!.Value), 1)
                        : null,
                Rereads = finishes.Count(f =>
                    earlierFinishes.Any(e =>
                        e.LibraryEntryId == f.LibraryEntryId && e.FinishedDate < f.FinishedDate
                    )
                ),
                Months = Enumerable
                    .Range(1, 12)
                    .Select(month => books.Count(b => b.FinishedDate.Month == month))
                    .ToArray(),
                Formats = BookFormat.All.ToDictionary(
                    format => format.Length == 0 ? "unset" : format,
                    format => finishes.Count(f => f.Format == format)
                ),
                Longest = byLength.LastOrDefault(),
                Shortest = byLength.Count >= 2 ? byLength.First() : null,
                TopAuthor = topAuthor,
                FiveStars = ratedBooks.Where(b => b.Rating == 5).ToList(),
                Books = books,
            }
        );
    }
}

public class YearBookResponse
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public int? PageCount { get; set; }
    public DateTime FinishedDate { get; set; }
    public int? Rating { get; set; }
}

public class AuthorCountResponse
{
    public string Name { get; set; } = string.Empty;
    public int Books { get; set; }
}

public class YearInBooksResponse
{
    public int Year { get; set; }
    public int? Target { get; set; }
    public int BooksRead { get; set; }
    public int PagesRead { get; set; }
    public int BooksWithoutPageCount { get; set; }
    public double? AverageRating { get; set; }
    public int Rereads { get; set; }
    public int[] Months { get; set; } = [];
    public Dictionary<string, int> Formats { get; set; } = [];
    public YearBookResponse? Longest { get; set; }
    public YearBookResponse? Shortest { get; set; }
    public AuthorCountResponse? TopAuthor { get; set; }
    public List<YearBookResponse> FiveStars { get; set; } = [];
    public List<YearBookResponse> Books { get; set; } = [];
}
