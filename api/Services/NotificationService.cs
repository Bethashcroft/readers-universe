using Microsoft.AspNetCore.SignalR;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Hubs;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public class NotificationService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<ChatHub> _hub;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext context,
        IHubContext<ChatHub> hub,
        ILogger<NotificationService> logger
    )
    {
        _context = context;
        _hub = hub;
        _logger = logger;
    }

    public async Task BorrowDeclinedAsync(IEnumerable<BorrowRequest> declined, int bookId)
    {
        foreach (var request in declined)
        {
            await AddAsync(
                request.FromUserId,
                request.ToUserId,
                NotificationTypes.BorrowDeclined,
                bookId: bookId
            );
        }
    }

    public async Task AddAsync(
        string userId,
        string actorId,
        string type,
        int? activityId = null,
        int? bookId = null
    )
    {
        if (userId == actorId)
        {
            return;
        }

        _context.Notifications.Add(
            new Notification
            {
                UserId = userId,
                ActorId = actorId,
                Type = type,
                ActivityId = activityId,
                BookId = bookId,
            }
        );

        await _context.SaveChangesAsync();

        try
        {
            await _hub.Clients.Group($"user-{userId}").SendAsync("NotificationReceived");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not push a notification to user {UserId}", userId);
        }
    }
}
