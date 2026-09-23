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
    private readonly ActivityService _activity;
    private readonly NotificationService _notifications;
    private readonly ReadingHistory _readings;

    public LibraryController(
        AppDbContext context,
        LendingService lending,
        ActivityService activity,
        NotificationService notifications,
        ReadingHistory readings
    )
    {
        _context = context;
        _lending = lending;
        _activity = activity;
        _notifications = notifications;
        _readings = readings;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyLibrary(
        [FromQuery] string? shelf,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] bool reverse,
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
                e.Offer == BookOffer.None
                && e.Shelf != BookShelf.WantToRead
                && !BookFormat.CannotOffer.Contains(e.Format)
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
            LibrarySort.Title => query.SortBy(e => e.Book.Title, descending: reverse),
            LibrarySort.Author => query
                .SortBy(e => e.Book.Author, descending: reverse)
                .ThenBy(e => e.Book.Title),
            LibrarySort.Rating => query
                .OrderBy(e =>
                    myRatings.Any(r => r.BookId == e.BookId && r.Rating != null) ? 0 : 1
                )
                .ThenSortBy(
                    e =>
                        myRatings
                            .Where(r => r.BookId == e.BookId)
                            .Select(r => r.Rating)
                            .FirstOrDefault(),
                    descending: !reverse
                )
                .ThenBy(e => e.Book.Title),
            LibrarySort.Finished => query
                .OrderBy(e => e.Readings.Any() ? 0 : 1)
                .ThenSortBy(
                    e => e.Readings.Max(r => (DateTime?)r.FinishedDate),
                    descending: !reverse
                )
                .ThenBy(e => e.Book.Title),
            _ => query.SortBy(e => e.Id, descending: !reverse),
        };

        var entries = await ordered
            .ThenBy(e => e.Id)
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

        var validationError =
            ValidateStates(request.Shelf, request.Offer)
            ?? ValidateFormat(request.Format, request.Offer);

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
            Format = request.Format,
            PageCount = request.PageCount > 0 ? request.PageCount : null,
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

        var shelfEvent = ActivityService.ForShelf(request.Shelf);

        if (shelfEvent != null)
        {
            _activity.Record(userId!, shelfEvent, book.Id, rating: request.Rating);
        }

        if (request.Offer == BookOffer.AvailableToBorrow)
        {
            _activity.Record(userId!, ActivityTypes.Offered, book.Id);
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

        var format = request.Format ?? entry.Format;

        var validationError =
            (
                offerChanging
                    ? ValidateStates(request.Shelf, request.Offer)
                    : ValidateShelf(request.Shelf)
            ) ?? ValidateFormat(format, request.Offer);

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

        var declined = leavingPool
            ? await _lending.DeclinePendingAsync(entry.Id)
            : [];

        if (request.Shelf != entry.Shelf)
        {
            var finish =
                request.Shelf == BookShelf.Read
                    ? await _readings.RecordAsync(entry.Id, ReadingHistory.TodayFor(request.Today))
                    : null;

            var shelfEvent = ActivityService.ForShelf(request.Shelf);

            if (shelfEvent != null)
            {
                var rating = await _context
                    .Reviews.Where(r => r.BookId == entry.BookId && r.UserId == userId)
                    .Select(r => r.Rating)
                    .FirstOrDefaultAsync();

                _activity.Record(
                    userId!,
                    shelfEvent,
                    entry.BookId,
                    rating: rating,
                    finish: finish
                );
            }
        }

        if (enteringPool)
        {
            _activity.Record(userId!, ActivityTypes.Offered, entry.BookId);
        }

        entry.Shelf = request.Shelf;
        entry.Offer = request.Offer;
        entry.Format = format;
        await _context.SaveChangesAsync();

        await _notifications.BorrowDeclinedAsync(declined, entry.BookId);

        return Ok((await ToResponsesAsync([entry], userId!)).Single());
    }

    [HttpPut("{id}/progress")]
    public async Task<IActionResult> UpdateProgress(
        int id,
        [FromBody] UpdateProgressRequest request
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var entry = await _context
            .LibraryEntries.Include(e => e.Book)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry == null)
        {
            return NotFound(new { message = "Book not found on your shelves" });
        }

        if (request.Page < 0 || request.PageCount is < 1)
        {
            return BadRequest(new { message = "Pages need to be positive numbers." });
        }

        if (request.Page > request.PageCount)
        {
            return BadRequest(new { message = "You can't be past the last page." });
        }

        var pageChanged = request.Page != entry.Page;

        entry.Page = request.Page;
        entry.PageCount = request.PageCount;

        if (pageChanged && request.Page > 0)
        {
            _activity.Record(
                userId!,
                ActivityTypes.Progress,
                entry.BookId,
                page: request.Page,
                pageCount: request.PageCount
            );
        }

        await _context.SaveChangesAsync();

        return Ok((await ToResponsesAsync([entry], userId!)).Single());
    }

    [HttpPost("{id}/page-count")]
    public async Task<IActionResult> LookUpPageCount(int id, [FromServices] IBookLookup lookup)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var entry = await _context
            .LibraryEntries.Include(e => e.Book)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry == null)
        {
            return NotFound(new { message = "Book not found on your shelves" });
        }

        if (entry.PageCount == null)
        {
            var found = await lookup.LookupAsync(entry.Book.Isbn);
            var pageCount =
                found?.PageCount
                ?? await lookup.FindPageCountAsync(entry.Book.Title, entry.Book.Author);

            if (pageCount != null)
            {
                entry.PageCount = pageCount;
                await _context.SaveChangesAsync();
            }
        }

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

        if (!LendingService.CanOffer(entry.Format))
        {
            return BadRequest(new { message = BookFormat.CannotOfferMessage });
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
            _activity.Record(userId!, ActivityTypes.Offered, entry.BookId);
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
            var declined = await _lending.DeclinePendingAsync(entry.Id);
            entry.Offer = BookOffer.None;
            await _context.SaveChangesAsync();
            await _notifications.BorrowDeclinedAsync(declined, entry.BookId);
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

        var declined = await _lending.DeclinePendingAsync(entry.Id);
        var bookId = entry.BookId;

        _context.LibraryEntries.Remove(entry);
        await _context.SaveChangesAsync();

        await _notifications.BorrowDeclinedAsync(declined, bookId);

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
    ) => LibraryEntryMapper.MapAsync(_context, entries, _ => viewerId, viewerId);

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

    private static string? ValidateFormat(string format, string offer) =>
        !BookFormat.All.Contains(format)
            ? $"Format must be one of: {string.Join(", ", BookFormat.All.Skip(1))}"
        : offer != BookOffer.None && !LendingService.CanOffer(format)
            ? BookFormat.CannotOfferMessage
        : null;
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
    public string Format { get; set; } = BookFormat.Unknown;
    public int? Rating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
    public bool ContainsSpoiler { get; set; }
    public int? PageCount { get; set; }
}

public class UpdateProgressRequest
{
    public int? Page { get; set; }
    public int? PageCount { get; set; }
}

public class UpdateLibraryEntryRequest
{
    public string Shelf { get; set; } = string.Empty;
    public string Offer { get; set; } = BookOffer.None;
    public string? Format { get; set; }
    public DateTime? Today { get; set; }
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
    public string Format { get; set; } = BookFormat.Unknown;
    public int? Page { get; set; }
    public int? PageCount { get; set; }
    public DateTime? FinishedDate { get; set; }
    public int TimesRead { get; set; }
    public int? Rating { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerUserName { get; set; } = string.Empty;
    public string SellerVintedUrl { get; set; } = string.Empty;
    public bool AlreadyOnShelves { get; set; }
    public bool CanRequest { get; set; }
}
