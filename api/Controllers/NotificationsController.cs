using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var notifications = await _context
            .Notifications.Where(n => n.UserId == me)
            .OrderByDescending(n => n.Date)
            .Take(30)
            .Select(n => new NotificationResponse
            {
                Id = n.Id,
                Type = n.Type,
                IsRead = n.IsRead,
                Date = n.Date,
                ActorUserName = n.Actor.UserName!,
                ActorDisplayName = n.Actor.DisplayName,
                ActorAvatarUrl = n.Actor.AvatarUrl,
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var count = await _context.Notifications.CountAsync(n => n.UserId == me && !n.IsRead);

        return Ok(new { count });
    }

    [HttpDelete]
    public async Task<IActionResult> ClearAll()
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var mine = await _context.Notifications.Where(n => n.UserId == me).ToListAsync();

        _context.Notifications.RemoveRange(mine);
        await _context.SaveChangesAsync();

        return Ok(new { count = mine.Count });
    }

    [HttpPost("read")]
    public async Task<IActionResult> MarkAllRead()
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var unread = await _context
            .Notifications.Where(n => n.UserId == me && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new { count = unread.Count });
    }
}

public class NotificationResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime Date { get; set; }
    public string ActorUserName { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
    public string ActorAvatarUrl { get; set; } = string.Empty;
}
