using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;

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

        var myBooks = await _context.Books.CountAsync(b => b.UserId == userId);

        var nearby = await _context.Books.CountAsync(b =>
            b.UserId != userId
            && (b.Offer == "available-to-borrow" || b.Offer == "for-sale")
        );

        var pendingRequests = await _context.BorrowRequests.CountAsync(r =>
            r.ToUserId == userId && r.Status == "pending"
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
