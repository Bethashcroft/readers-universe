using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;

namespace ReadersRealm.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;

    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{Context.UserIdentifier}");
        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(int borrowRequestId)
    {
        var userId = Context.UserIdentifier;

        var isParty = await _context.BorrowRequests.AnyAsync(r =>
            r.Id == borrowRequestId && (r.FromUserId == userId || r.ToUserId == userId)
        );

        if (!isParty)
        {
            throw new HubException("You are not part of this conversation");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"request-{borrowRequestId}");
    }

    public async Task LeaveConversation(int borrowRequestId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"request-{borrowRequestId}");
    }
}
