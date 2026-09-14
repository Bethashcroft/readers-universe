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
public class BorrowingController : ControllerBase
{
    private const int HistoryLength = 20;

    private readonly AppDbContext _context;

    public BorrowingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var entries = await _context
            .LibraryEntries.Where(e => e.UserId == me && BookOffer.Lendable.Contains(e.Offer))
            .Include(e => e.Book)
            .OrderBy(e => e.Id)
            .ToListAsync();

        var entryIds = entries.Select(e => e.Id).ToList();

        var loans = await _context
            .BorrowRequests.Where(r =>
                entryIds.Contains(r.LibraryEntryId) && r.Status == BorrowStatus.Accepted
            )
            .OrderByDescending(r => r.Date)
            .Select(r => new
            {
                r.LibraryEntryId,
                Borrower = new BorrowerResponse
                {
                    RequestId = r.Id,
                    DisplayName = r.FromUser.DisplayName,
                    UserName = r.FromUser.UserName!,
                },
            })
            .ToListAsync();

        var borrowerByEntry = loans
            .GroupBy(l => l.LibraryEntryId)
            .ToDictionary(g => g.Key, g => g.First().Borrower);

        var offering = entries
            .Select(e => new OfferedBookResponse
            {
                LibraryEntryId = e.Id,
                BookId = e.BookId,
                Title = e.Book.Title,
                Author = e.Book.Author,
                CoverUrl = e.Book.CoverUrl,
                Offer = e.Offer,
                Borrower = borrowerByEntry.GetValueOrDefault(e.Id),
            })
            .ToList();

        var requests = await _context
            .BorrowRequests.Where(r => r.FromUserId == me || r.ToUserId == me)
            .OrderByDescending(r => r.Date)
            .ToResponses()
            .ToListAsync();

        return Ok(
            new BorrowingResponse
            {
                Offering = offering,
                Borrowed = requests
                    .Where(r => r.FromUserId == me && r.Status == BorrowStatus.Accepted)
                    .ToList(),
                Incoming = requests
                    .Where(r => r.ToUserId == me && r.Status == BorrowStatus.Pending)
                    .ToList(),
                Outgoing = requests
                    .Where(r => r.FromUserId == me && r.Status == BorrowStatus.Pending)
                    .ToList(),
                History = requests
                    .Where(r =>
                        r.Status == BorrowStatus.Declined || r.Status == BorrowStatus.Returned
                    )
                    .Take(HistoryLength)
                    .ToList(),
                Limit = LendingService.Max,
            }
        );
    }
}

public class BorrowerResponse
{
    public int RequestId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public class OfferedBookResponse
{
    public int LibraryEntryId { get; set; }
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string Offer { get; set; } = BookOffer.None;
    public BorrowerResponse? Borrower { get; set; }
}

public class BorrowingResponse
{
    public List<OfferedBookResponse> Offering { get; set; } = [];
    public List<BorrowRequestResponse> Borrowed { get; set; } = [];
    public List<BorrowRequestResponse> Incoming { get; set; } = [];
    public List<BorrowRequestResponse> Outgoing { get; set; } = [];
    public List<BorrowRequestResponse> History { get; set; } = [];
    public int Limit { get; set; }
}
