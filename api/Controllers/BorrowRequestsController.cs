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
public class BorrowRequestsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly LendingService _lending;
    private readonly NotificationService _notifications;

    public BorrowRequestsController(
        AppDbContext context,
        LendingService lending,
        NotificationService notifications
    )
    {
        _context = context;
        _lending = lending;
        _notifications = notifications;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateBorrowRequest request)
    {
        var fromUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var entry = await _context.LibraryEntries.FirstOrDefaultAsync(e =>
            e.Id == request.LibraryEntryId
        );

        if (entry == null)
        {
            return NotFound(new { message = "Book not found" });
        }

        if (entry.UserId == fromUserId)
        {
            return BadRequest(new { message = "You cannot request your own book" });
        }

        if (entry.Offer != BookOffer.AvailableToBorrow)
        {
            return BadRequest(new { message = "This book isn't available to borrow." });
        }

        var trusted = await _context.Trusts.AnyAsync(t =>
            t.TrusterId == entry.UserId && t.TrustedId == fromUserId
        );

        if (!trusted)
        {
            return StatusCode(
                403,
                new
                {
                    message = "Only readers in this owner's Trusted Book Club can borrow their books.",
                }
            );
        }

        var alreadyOnMyShelves = await _context.LibraryEntries.AnyAsync(e =>
            e.BookId == entry.BookId && e.UserId == fromUserId
        );

        if (alreadyOnMyShelves)
        {
            return BadRequest(new { message = "This book is already on your shelves." });
        }

        var alreadyRated = await _context.Reviews.AnyAsync(r =>
            r.BookId == entry.BookId && r.UserId == fromUserId
        );

        if (alreadyRated)
        {
            return BadRequest(
                new { message = "You've already rated this book, so you can't request it." }
            );
        }

        var alreadyRequested = await _context.BorrowRequests.AnyAsync(r =>
            r.LibraryEntryId == entry.Id
            && r.FromUserId == fromUserId
            && r.Status == BorrowStatus.Pending
        );

        if (alreadyRequested)
        {
            return BadRequest(new { message = "You already have a pending request for this book" });
        }

        var borrowRequest = new BorrowRequest
        {
            LibraryEntryId = entry.Id,
            FromUserId = fromUserId!,
            ToUserId = entry.UserId,
            Message = request.Message,
        };

        _context.BorrowRequests.Add(borrowRequest);
        await _context.SaveChangesAsync();

        await _notifications.AddAsync(
            entry.UserId,
            fromUserId!,
            NotificationTypes.BorrowRequested,
            bookId: entry.BookId
        );

        return Ok(await ResponseForAsync(borrowRequest.Id));
    }

    [HttpGet]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var requests = await _context
            .BorrowRequests.Where(r => r.FromUserId == userId || r.ToUserId == userId)
            .OrderByDescending(r => r.Date)
            .ToResponses()
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("pending-count")]
    public async Task<IActionResult> PendingCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var count = await _context.BorrowRequests.CountAsync(r =>
            r.ToUserId == userId && r.Status == BorrowStatus.Pending
        );

        return Ok(new { count });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> WithdrawRequest(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var borrowRequest = await _context.BorrowRequests.FindAsync(id);

        if (borrowRequest == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        if (borrowRequest.FromUserId != userId)
        {
            return Forbid();
        }

        if (borrowRequest.Status != BorrowStatus.Pending)
        {
            return BadRequest(new { message = "Only pending requests can be withdrawn" });
        }

        _context.BorrowRequests.Remove(borrowRequest);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateBorrowStatusRequest request
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (request.Status != BorrowStatus.Accepted && request.Status != BorrowStatus.Declined)
        {
            return BadRequest(new { message = "Status must be 'accepted' or 'declined'" });
        }

        var borrowRequest = await _context
            .BorrowRequests.Include(r => r.LibraryEntry)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (borrowRequest == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        if (borrowRequest.ToUserId != userId)
        {
            return Forbid();
        }

        if (borrowRequest.Status != BorrowStatus.Pending)
        {
            return BadRequest(new { message = "This request has already been answered." });
        }

        var declined = new List<BorrowRequest>();

        if (request.Status == BorrowStatus.Accepted)
        {
            var stillTrusted = await _context.Trusts.AnyAsync(t =>
                t.TrusterId == userId && t.TrustedId == borrowRequest.FromUserId
            );

            if (!stillTrusted)
            {
                return StatusCode(
                    403,
                    new { message = "This reader is no longer in your Trusted Book Club." }
                );
            }

            if (borrowRequest.LibraryEntry.Offer != BookOffer.AvailableToBorrow)
            {
                return BadRequest(
                    new { message = "This book isn't offered to borrow any more." }
                );
            }

            borrowRequest.LibraryEntry.Offer = BookOffer.LentOut;
            declined = await _lending.DeclinePendingAsync(
                borrowRequest.LibraryEntryId,
                exceptRequestId: borrowRequest.Id
            );
        }

        borrowRequest.Status = request.Status;
        await _context.SaveChangesAsync();

        var bookId = borrowRequest.LibraryEntry.BookId;

        await _notifications.AddAsync(
            borrowRequest.FromUserId,
            userId!,
            request.Status == BorrowStatus.Accepted
                ? NotificationTypes.BorrowAccepted
                : NotificationTypes.BorrowDeclined,
            bookId: bookId
        );

        await _notifications.BorrowDeclinedAsync(declined, bookId);

        return Ok(await ResponseForAsync(borrowRequest.Id));
    }

    [HttpPost("{id}/return")]
    public async Task<IActionResult> MarkReturned(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var borrowRequest = await _context
            .BorrowRequests.Include(r => r.LibraryEntry)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (borrowRequest == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        if (borrowRequest.ToUserId != userId)
        {
            return Forbid();
        }

        if (borrowRequest.Status != BorrowStatus.Accepted)
        {
            return BadRequest(
                new { message = "Only a book that's out on loan can be marked returned." }
            );
        }

        borrowRequest.Status = BorrowStatus.Returned;

        if (borrowRequest.LibraryEntry.Offer == BookOffer.LentOut)
        {
            borrowRequest.LibraryEntry.Offer = BookOffer.AvailableToBorrow;
        }

        await _context.SaveChangesAsync();

        await _notifications.AddAsync(
            borrowRequest.FromUserId,
            userId!,
            NotificationTypes.BorrowReturned,
            bookId: borrowRequest.LibraryEntry.BookId
        );

        return Ok(await ResponseForAsync(borrowRequest.Id));
    }

    private Task<BorrowRequestResponse> ResponseForAsync(int id) =>
        _context.BorrowRequests.Where(r => r.Id == id).ToResponses().SingleAsync();
}

public class CreateBorrowRequest
{
    public int LibraryEntryId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BorrowRequestResponse
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string FromUserId { get; set; } = string.Empty;
    public string FromUserName { get; set; } = string.Empty;
    public string ToUserId { get; set; } = string.Empty;
    public string ToUserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class UpdateBorrowStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
