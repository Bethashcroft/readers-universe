using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;
using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IBookLookup _lookup;

    public BooksController(AppDbContext context, IBookLookup lookup)
    {
        _context = context;
        _lookup = lookup;
    }

    [HttpGet("lookup/{isbn}")]
    public async Task<IActionResult> Lookup(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return BadRequest(new { message = "Enter an ISBN." });
        }

        var result = await _lookup.LookupAsync(isbn);

        if (result == null)
        {
            return NotFound(
                new { message = "No book found for that ISBN. You can enter the details by hand." }
            );
        }

        return Ok(result);
    }

    [HttpGet("browse")]
    public async Task<IActionResult> Browse(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        [FromQuery] string? offer
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (currentPage, size) = PagedResult<LibraryEntryResponse>.Normalise(page, pageSize);

        var query = _context.LibraryEntries.Where(e =>
            e.Offer == BookOffer.AvailableToBorrow || e.Offer == BookOffer.ForSale
        );

        if (offer == BookOffer.AvailableToBorrow || offer == BookOffer.ForSale)
        {
            query = query.Where(e => e.Offer == offer);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.Book.Title.ToLower().Contains(term) || e.Book.Author.ToLower().Contains(term)
            );
        }

        var total = await query.CountAsync();

        var entries = await query
            .Include(e => e.Book)
            .Include(e => e.User)
            .OrderByDescending(e => e.Id)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync();

        var responses = await LibraryEntryMapper.MapAsync(_context, entries, e => e.UserId);

        if (userId != null)
        {
            var bookIds = entries.Select(e => e.BookId).Distinct().ToList();

            var mine = (
                await _context
                    .LibraryEntries.Where(e => e.UserId == userId && bookIds.Contains(e.BookId))
                    .Select(e => e.BookId)
                    .ToListAsync()
            ).ToHashSet();

            foreach (var response in responses)
            {
                response.AlreadyOnShelves = mine.Contains(response.BookId);
            }
        }

        return Ok(PagedResult<LibraryEntryResponse>.From(responses, currentPage, size, total));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBook(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            return NotFound(new { message = "Book not found" });
        }

        var ratings = await _context
            .Reviews.Where(r => r.BookId == id && r.Rating != null)
            .Select(r => r.Rating!.Value)
            .ToListAsync();

        var myEntry = await _context
            .LibraryEntries.Include(e => e.Book)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.BookId == id && e.UserId == userId);

        var owners = await _context
            .LibraryEntries.Where(e =>
                e.BookId == id
                && e.UserId != userId
                && (e.Offer == BookOffer.AvailableToBorrow || e.Offer == BookOffer.ForSale)
            )
            .Include(e => e.User)
            .Select(e => new BookOwnerResponse
            {
                LibraryEntryId = e.Id,
                UserName = e.User.UserName!,
                DisplayName = e.User.DisplayName,
                Offer = e.Offer,
                SellerVintedUrl = e.User.VintedUrl,
            })
            .ToListAsync();

        return Ok(
            new BookDetailResponse
            {
                Owners = owners,
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                CoverUrl = book.CoverUrl,
                Isbn = book.Isbn,
                AverageRating = ratings.Count > 0 ? Math.Round(ratings.Average(), 1) : null,
                RatingCount = ratings.Count,
                MyEntry =
                    myEntry == null
                        ? null
                        : (
                            await LibraryEntryMapper.MapAsync(_context, [myEntry], e => e.UserId)
                        ).Single(),
            }
        );
    }
}

public class BookDetailResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public double? AverageRating { get; set; }
    public int RatingCount { get; set; }
    public LibraryEntryResponse? MyEntry { get; set; }
    public List<BookOwnerResponse> Owners { get; set; } = [];
}

public class BookOwnerResponse
{
    public int LibraryEntryId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Offer { get; set; } = BookOffer.None;
    public string SellerVintedUrl { get; set; } = string.Empty;
}
