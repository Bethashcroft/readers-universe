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
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var myBooks = await _context.LibraryEntries.CountAsync(e => e.UserId == userId);

        var nearby = await _context
            .LibraryEntries.Where(e =>
                e.UserId != userId
                && (e.Offer == BookOffer.AvailableToBorrow || e.Offer == BookOffer.ForSale)
            )
            .VisibleTo(_context, userId)
            .CountAsync();

        var pendingRequests = await _context.BorrowRequests.CountAsync(r =>
            r.ToUserId == userId && r.Status == BorrowStatus.Pending
        );

        return Ok(
            new DashboardSummaryResponse
            {
                MyBooks = myBooks,
                Nearby = nearby,
                PendingRequests = pendingRequests,
            }
        );
    }
}

public class DashboardSummaryResponse
{
    public int MyBooks { get; set; }
    public int Nearby { get; set; }
    public int PendingRequests { get; set; }
}
