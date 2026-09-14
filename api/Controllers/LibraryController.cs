using System.ComponentModel.DataAnnotations;
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
public class LibraryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly LendingService _lending;

    public LibraryController(AppDbContext context, LendingService lending)
    {
        _context = context;
        _lending = lending;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyLibrary(
        [FromQuery] string? shelf,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] bool offerable,
        [FromQuery] int? page,
        [FromQuery] int? pageSize
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(shelf) && !BookShelf.All.Contains(shelf))
        {
            return BadRequest(new { message = "That is not a shelf." });
        }

        if (!string.IsNullOrEmpty(sort) && !LibrarySort.All.Contains(sort))
        {
            return BadRequest(new { message = "That is not a way to sort your shelves." });
        }

        var (currentPage, size) = PagedResult<LibraryEntryResponse>.Normalise(page, pageSize);

        var query = _context.LibraryEntries.Where(e => e.UserId == userId);

        if (!string.IsNullOrEmpty(shelf))
        {
            query = query.Where(e => e.Shelf == shelf);
        }

        if (offerable)
        {
            query = query.Where(e =>
                e.Offer == BookOffer.None && e.Shelf != BookShelf.WantToRead
            );
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.Book.Title.ToLower().Contains(term) || e.Book.Author.ToLower().Contains(term)
            );
        }

        var total = await query.CountAsync();

        var myRatings = _context.Reviews.Where(r => r.UserId == userId);

        var ordered = sort switch
        {
            LibrarySort.Title => query.OrderBy(e => e.Book.Title).ThenBy(e => e.Id),
            LibrarySort.Author => query
                .OrderBy(e => e.Book.Author)
                .ThenBy(e => e.Book.Title)
                .ThenBy(e => e.Id),
            LibrarySort.Rating => query
                .OrderByDescending(e =>
                    myRatings.Where(r => r.BookId == e.BookId).Select(r => r.Rating).FirstOrDefault()
                        ?? 0
                )
                .ThenBy(e => e.Book.Title)
                .ThenBy(e => e.Id),
            _ => query.OrderByDescending(e => e.Id),
        };

        var entries = await ordered
            .Include(e => e.Book)
            .Include(e => e.User)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync();

        return Ok(
            PagedResult<LibraryEntryResponse>.From(
                await ToResponsesAsync(entries, userId!),
                currentPage,
                size,
                total
            )
        );
    }

    [HttpPost("refresh-covers")]
    public async Task<IActionResult> RefreshCovers(
        [FromServices] CoverService covers,
        [FromServices] CoverBackfillLimiter limiter,
        [FromQuery] int afterId = 0,
        [FromQuery] int max = 25,
        [FromQuery] bool withTotal = false,
        CancellationToken cancellationToken = default
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await limiter.RunAsync(
            userId!,
            () =>
                covers.BackfillAsync(
                    userId!,
                    Math.Max(afterId, 0),
                    Math.Clamp(max, 1, 25),
                    withTotal,
                    cancellationToken
                )
        );

        if (result == null)
        {
            return Conflict(
                new { message = "A cover search is already running. Let that one finish first." }
            );
        }

        return Ok(result);
    }

    [HttpGet("shelf-counts")]
    public async Task<IActionResult> GetShelfCounts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var counts = await _context
            .LibraryEntries.Where(e => e.UserId == userId)
            .GroupBy(e => e.Shelf)
            .Select(g => new { Shelf = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(counts.ToDictionary(c => c.Shelf, c => c.Count));
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

        if (request.Offer == BookOffer.AvailableToBorrow)
        {
            if (request.Shelf == BookShelf.WantToRead)
            {
                return BadRequest(new { message = LendingService.NotOwnedMessage });
            }

            if (!await _lending.HasRoomAsync(userId!))
            {
                return BadRequest(new { message = LendingService.FullMessage });
            }
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

        if (request.Rating.HasValue || !string.IsNullOrWhiteSpace(request.ReviewText))
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
                        Rating = request.Rating,
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

        var offerChanging = request.Offer != entry.Offer;

        var validationError = offerChanging
            ? ValidateStates(request.Shelf, request.Offer)
            : ValidateShelf(request.Shelf);

        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        if (offerChanging && await _lending.OnLoanAsync(entry.Id))
        {
            return BadRequest(new { message = LendingService.OnLoanMessage });
        }

        if (BookOffer.Lendable.Contains(request.Offer) && request.Shelf == BookShelf.WantToRead)
        {
            return BadRequest(new { message = LendingService.NotOwnedMessage });
        }

        var enteringPool =
            request.Offer == BookOffer.AvailableToBorrow
            && !BookOffer.Lendable.Contains(entry.Offer);

        if (enteringPool && !await _lending.HasRoomAsync(userId!))
        {
            return BadRequest(new { message = LendingService.FullMessage });
        }

        var leavingPool =
            BookOffer.Lendable.Contains(entry.Offer)
            && request.Offer != BookOffer.AvailableToBorrow;

        if (leavingPool)
        {
            await _lending.DeclinePendingAsync(entry.Id);
        }

        entry.Shelf = request.Shelf;
        entry.Offer = request.Offer;
        await _context.SaveChangesAsync();

        return Ok((await ToResponsesAsync([entry], userId!)).Single());
    }

    [HttpPost("{id}/offer")]
    public async Task<IActionResult> OfferBook(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var entry = await _context.LibraryEntries.FirstOrDefaultAsync(e =>
            e.Id == id && e.UserId == userId
        );

        if (entry == null)
        {
            return NotFound(new { message = "Book not found on your shelves" });
        }

        if (entry.Shelf == BookShelf.WantToRead)
        {
            return BadRequest(new { message = LendingService.NotOwnedMessage });
        }

        if (entry.Offer == BookOffer.ForSale)
        {
            return BadRequest(
                new { message = "This book is for sale. Take it off sale before offering it." }
            );
        }

        if (entry.Offer == BookOffer.None)
        {
            if (!await _lending.HasRoomAsync(userId!))
            {
                return BadRequest(new { message = LendingService.FullMessage });
            }

            entry.Offer = BookOffer.AvailableToBorrow;
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpDelete("{id}/offer")]
    public async Task<IActionResult> TakeBackBook(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var entry = await _context.LibraryEntries.FirstOrDefaultAsync(e =>
            e.Id == id && e.UserId == userId
        );

        if (entry == null)
        {
            return NotFound(new { message = "Book not found on your shelves" });
        }

        if (entry.Offer == BookOffer.LentOut || await _lending.OnLoanAsync(entry.Id))
        {
            return BadRequest(new { message = LendingService.OnLoanMessage });
        }

        if (entry.Offer == BookOffer.AvailableToBorrow)
        {
            await _lending.DeclinePendingAsync(entry.Id);
            entry.Offer = BookOffer.None;
            await _context.SaveChangesAsync();
        }

        return Ok();
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

        if (await _lending.OnLoanAsync(entry.Id))
        {
            return BadRequest(
                new { message = "This book is out on loan. Mark it returned before removing it." }
            );
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

        if (CoverPolicy.ShouldReplaceCover(book, request.CoverUrl, speculative: false))
        {
            CoverPolicy.ApplyCover(book, request.CoverUrl);
        }

        return book;
    }

    private Task<List<LibraryEntryResponse>> ToResponsesAsync(
        List<LibraryEntry> entries,
        string viewerId
    ) => LibraryEntryMapper.MapAsync(_context, entries, _ => viewerId);

    public static string? ValidateStates(string shelf, string offer) =>
        ValidateShelf(shelf)
        ?? (
            BookOffer.Selectable.Contains(offer)
                ? null
                : $"Offer must be one of: {string.Join(", ", BookOffer.Selectable)}"
        );

    private static string? ValidateShelf(string shelf) =>
        BookShelf.All.Contains(shelf)
            ? null
            : $"Shelf must be one of: {string.Join(", ", BookShelf.All)}";
}

public class AddToLibraryRequest
{
    public int? BookId { get; set; }

    [MaxLength(300, ErrorMessage = "Title must be 300 characters or fewer.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "Author must be 200 characters or fewer.")]
    public string Author { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Cover URL is too long.")]
    public string CoverUrl { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "ISBN is too long.")]
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
    public bool AlreadyOnShelves { get; set; }
    public bool CanRequest { get; set; }
}
