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
public class BorrowRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BorrowRequestsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateBorrowRequest request)
    {
        var fromUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var entry = await _context
            .LibraryEntries.Include(e => e.Book)
            .FirstOrDefaultAsync(e => e.Id == request.LibraryEntryId);

        if (entry == null)
        {
            return NotFound(new { message = "Book not found" });
        }

        if (entry.UserId == fromUserId)
        {
            return BadRequest(new { message = "You cannot request your own book" });
        }

        var alreadyOnMyShelves = await _context.LibraryEntries.AnyAsync(e =>
            e.BookId == entry.BookId && e.UserId == fromUserId
        );

        if (alreadyOnMyShelves)
        {
            return BadRequest(
                new { message = "This book is already on your shelves." }
            );
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

        var fromUser = await _context.Users.FindAsync(fromUserId);

        return Ok(
            ToResponse(
                borrowRequest,
                entry.BookId,
                entry.Book.Title,
                fromUser?.DisplayName ?? "Unknown"
            )
        );
    }

    [HttpGet]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var requests = await _context
            .BorrowRequests.Where(r => r.FromUserId == userId || r.ToUserId == userId)
            .Include(r => r.LibraryEntry)
            .ThenInclude(e => e.Book)
            .Select(r => new BorrowRequestResponse
            {
                Id = r.Id,
                BookId = r.LibraryEntry.BookId,
                BookTitle = r.LibraryEntry.Book.Title,
                FromUserId = r.FromUserId,
                FromUserName = r.FromUser.DisplayName,
                ToUserId = r.ToUserId,
                Status = r.Status,
                Message = r.Message,
                Date = r.Date,
            })
            .OrderByDescending(r => r.Date)
            .ToListAsync();

        return Ok(requests);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRequest(int id)
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
            .ThenInclude(e => e.Book)
            .Include(r => r.FromUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (borrowRequest == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        if (borrowRequest.ToUserId != userId)
        {
            return Forbid();
        }

        borrowRequest.Status = request.Status;

        if (request.Status == BorrowStatus.Accepted)
        {
            borrowRequest.LibraryEntry.Offer = BookOffer.LentOut;

            var competing = await _context
                .BorrowRequests.Where(r =>
                    r.LibraryEntryId == borrowRequest.LibraryEntryId
                    && r.Id != borrowRequest.Id
                    && r.Status == BorrowStatus.Pending
                )
                .ToListAsync();

            foreach (var other in competing)
            {
                other.Status = BorrowStatus.Declined;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(
            ToResponse(
                borrowRequest,
                borrowRequest.LibraryEntry.BookId,
                borrowRequest.LibraryEntry.Book.Title,
                borrowRequest.FromUser.DisplayName
            )
        );
    }

    private static BorrowRequestResponse ToResponse(
        BorrowRequest request,
        int bookId,
        string bookTitle,
        string fromUserName
    ) =>
        new()
        {
            Id = request.Id,
            BookId = bookId,
            BookTitle = bookTitle,
            FromUserId = request.FromUserId,
            FromUserName = fromUserName,
            ToUserId = request.ToUserId,
            Status = request.Status,
            Message = request.Message,
            Date = request.Date,
        };
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
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class UpdateBorrowStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
