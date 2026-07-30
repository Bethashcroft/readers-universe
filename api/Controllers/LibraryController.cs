using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LibraryController : ControllerBase
{
    private readonly AppDbContext _context;

    public LibraryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyLibrary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var entries = await _context
            .LibraryEntries.Where(e => e.UserId == userId)
            .Include(e => e.Book)
            .Include(e => e.User)
            .OrderByDescending(e => e.Id)
            .ToListAsync();

        return Ok(await ToResponsesAsync(entries, userId!));
    }

    [HttpPost]
    public async Task<IActionResult> AddToLibrary([FromBody] AddToLibraryRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (request.BookId == null && string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "A title is required." });
        }

        var validationError = ValidateStates(request.Shelf, request.Offer);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        if (request.Rating is < 1 or > 5)
        {
            return BadRequest(new { message = "Rating must be between 1 and 5" });
        }

        var book = await FindOrCreateBookAsync(request);

        if (book == null)
        {
            return NotFound(new { message = "Book not found" });
        }

        var alreadyOnShelf = await _context.LibraryEntries.AnyAsync(e =>
            e.UserId == userId && e.BookId == book.Id
        );
        if (alreadyOnShelf)
        {
            return BadRequest(new { message = "That book is already on your shelves." });
        }

        var entry = new LibraryEntry
        {
            BookId = book.Id,
            UserId = userId!,
            Shelf = request.Shelf,
            Offer = request.Offer,
        };
        _context.LibraryEntries.Add(entry);

        if (request.Rating.HasValue)
        {
            var alreadyReviewed = await _context.Reviews.AnyAsync(r =>
                r.BookId == book.Id && r.UserId == userId
            );

            if (!alreadyReviewed)
            {
                _context.Reviews.Add(
                    new Review
                    {
                        BookId = book.Id,
                        UserId = userId!,
                        Rating = request.Rating.Value,
                        Text = request.ReviewText ?? string.Empty,
                        ContainsSpoiler = request.ContainsSpoiler,
                    }
                );
            }
        }

        await _context.SaveChangesAsync();

        entry.Book = book;
        entry.User = (await _context.Users.FindAsync(userId))!;

        return Ok((await ToResponsesAsync([entry], userId!)).Single());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEntry(int id, [FromBody] UpdateLibraryEntryRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var validationError = ValidateStates(request.Shelf, request.Offer);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        var entry = await _context
            .LibraryEntries.Include(e => e.Book)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry == null)
        {
            return NotFound(new { message = "Book not found on your shelves" });
        }

        if (entry.UserId != userId)
        {
            return Forbid();
        }

        entry.Shelf = request.Shelf;
        entry.Offer = request.Offer;
        await _context.SaveChangesAsync();

        return Ok((await ToResponsesAsync([entry], userId!)).Single());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveEntry(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var entry = await _context.LibraryEntries.FindAsync(id);

        if (entry == null)
        {
            return NotFound(new { message = "Book not found on your shelves" });
        }

        if (entry.UserId != userId)
        {
            return Forbid();
        }

        _context.LibraryEntries.Remove(entry);
        await _context.SaveChangesAsync();

        return Ok();
    }

    private async Task<Book?> FindOrCreateBookAsync(AddToLibraryRequest request)
    {
        if (request.BookId.HasValue)
        {
            return await _context.Books.FindAsync(request.BookId.Value);
        }

        var isbn = Book.CleanIsbn(request.Isbn ?? string.Empty);
        var matchKey = Book.BuildMatchKey(request.Title, request.Author);

        Book? book = null;

        if (!string.IsNullOrEmpty(isbn))
        {
            book = await _context.Books.FirstOrDefaultAsync(b => b.Isbn == isbn);
        }

        book ??= await _context.Books.FirstOrDefaultAsync(b => b.MatchKey == matchKey);

        if (book == null)
        {
            book = new Book
            {
                Title = request.Title.Trim(),
                Author = request.Author.Trim(),
                CoverUrl = request.CoverUrl,
                Isbn = isbn,
                MatchKey = matchKey,
            };
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return book;
        }

        if (string.IsNullOrEmpty(book.Isbn) && !string.IsNullOrEmpty(isbn))
        {
            book.Isbn = isbn;
        }

        if (!string.IsNullOrEmpty(request.CoverUrl) && book.CoverUrl.Contains("placehold.co"))
        {
            book.CoverUrl = request.CoverUrl;
        }

        return book;
    }

    private Task<List<LibraryEntryResponse>> ToResponsesAsync(
        List<LibraryEntry> entries,
        string viewerId
    ) => LibraryEntryMapper.MapAsync(_context, entries, _ => viewerId);

    public static string? ValidateStates(string shelf, string offer)
    {
        if (!BookShelf.All.Contains(shelf))
        {
            return $"Shelf must be one of: {string.Join(", ", BookShelf.All)}";
        }

        if (!BookOffer.All.Contains(offer))
        {
            return $"Offer must be one of: {string.Join(", ", BookOffer.All)}";
        }

        return null;
    }
}

public class AddToLibraryRequest
{
    public int? BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Shelf { get; set; } = string.Empty;
    public string Offer { get; set; } = BookOffer.None;
    public int? Rating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
    public bool ContainsSpoiler { get; set; }
}

public class UpdateLibraryEntryRequest
{
    public string Shelf { get; set; } = string.Empty;
    public string Offer { get; set; } = BookOffer.None;
}

public class LibraryEntryResponse
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Shelf { get; set; } = string.Empty;
    public string Offer { get; set; } = BookOffer.None;
    public int? Rating { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerUserName { get; set; } = string.Empty;
    public string SellerVintedUrl { get; set; } = string.Empty;
}
