using Microsoft.AspNetCore.SignalR;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Hubs;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public class NotificationService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<ChatHub> _hub;

    public NotificationService(AppDbContext context, IHubContext<ChatHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    public async Task AddAsync(string userId, string actorId, string type)
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
            }
        );

        await _context.SaveChangesAsync();

        await _hub.Clients.Group($"user-{userId}").SendAsync("NotificationReceived");
    }
}
