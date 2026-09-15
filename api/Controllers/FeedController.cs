using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeedController : ControllerBase
{
    private readonly AppDbContext _context;

    public FeedController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var following = _context
            .Follows.Where(f => f.FollowerId == me && f.Approved)
            .Select(f => f.FollowingId);

        var feed = await _context
            .Activities.Where(a => following.Contains(a.UserId))
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.Id)
            .Take(ActivityMapper.PageLength)
            .ToResponses()
            .ToListAsync();

        return Ok(feed);
    }
}
