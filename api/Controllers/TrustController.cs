using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;
using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class TrustController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly NotificationService _notifications;

    public TrustController(
        AppDbContext context,
        UserManager<AppUser> userManager,
        NotificationService notifications
    )
    {
        _context = context;
        _userManager = userManager;
        _notifications = notifications;
    }

    [HttpPost("{username}/trust")]
    public async Task<IActionResult> TrustReader(string username)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var them = await _userManager.FindByNameAsync(username);

        if (them == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (them.Id == me)
        {
            return BadRequest(new { message = "You cannot add yourself." });
        }

        var existing = await _context.Trusts.AnyAsync(t =>
            t.TrusterId == me && t.TrustedId == them.Id
        );

        if (!existing)
        {
            _context.Trusts.Add(new Trust { TrusterId = me!, TrustedId = them.Id });

            if (await _context.TrySaveChangesAsync())
            {
                await _notifications.AddAsync(them.Id, me!, NotificationTypes.Trusted);
            }
        }

        return Ok(new { trusted = true });
    }

    [HttpDelete("{username}/trust")]
    public async Task<IActionResult> UntrustReader(string username)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var them = await _userManager.FindByNameAsync(username);

        if (them == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var existing = await _context.Trusts.FirstOrDefaultAsync(t =>
            t.TrusterId == me && t.TrustedId == them.Id
        );

        if (existing != null)
        {
            _context.Trusts.Remove(existing);

            var stale = await _context
                .Notifications.Where(n =>
                    n.UserId == them.Id
                    && n.ActorId == me
                    && n.Type == NotificationTypes.Trusted
                )
                .ToListAsync();

            _context.Notifications.RemoveRange(stale);

            var pendingRequests = await _context
                .BorrowRequests.Where(r =>
                    r.FromUserId == them.Id
                    && r.ToUserId == me
                    && r.Status == BorrowStatus.Pending
                )
                .ToListAsync();

            foreach (var pending in pendingRequests)
            {
                pending.Status = BorrowStatus.Declined;
            }

            await _context.SaveChangesAsync();
        }

        return Ok(new { trusted = false });
    }

    [HttpGet("trusted")]
    public async Task<IActionResult> TrustedReaders()
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var readers = await _context
            .Trusts.Where(t => t.TrusterId == me)
            .OrderBy(t => t.Trusted.DisplayName)
            .Select(t => new TrustedReaderResponse
            {
                UserName = t.Trusted.UserName!,
                DisplayName = t.Trusted.DisplayName,
                AvatarUrl = t.Trusted.AvatarUrl,
                Bio = t.Trusted.Bio,
                Date = t.Date,
            })
            .ToListAsync();

        return Ok(readers);
    }
}

public class TrustedReaderResponse
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
