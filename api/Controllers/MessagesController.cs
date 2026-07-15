using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Hubs;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<ChatHub> _hub;

    public MessagesController(AppDbContext context, IHubContext<ChatHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { message = "Message can't be empty" });
        }

        if (request.Text.Length > 1000)
        {
            return BadRequest(new { message = "Message is too long (1000 characters max)" });
        }

        var borrowRequest = await _context.BorrowRequests.FindAsync(request.BorrowRequestId);

        if (borrowRequest == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        if (borrowRequest.FromUserId != userId && borrowRequest.ToUserId != userId)
        {
            return Forbid();
        }

        var message = new Message
        {
            Text = request.Text.Trim(),
            BorrowRequestId = borrowRequest.Id,
            SenderId = userId!,
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var sender = await _context.Users.FindAsync(userId);
        var response = ToResponse(message, sender?.DisplayName ?? "Unknown");

        var recipientId =
            borrowRequest.FromUserId == userId
                ? borrowRequest.ToUserId
                : borrowRequest.FromUserId;

        await _hub.Clients.Group($"request-{borrowRequest.Id}").SendAsync("NewMessage", response);
        await _hub
            .Clients.Group($"user-{recipientId}")
            .SendAsync("MessageReceived", new { borrowRequestId = borrowRequest.Id });

        return Ok(response);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var count = await _context.Messages.CountAsync(m =>
            !m.IsRead
            && m.SenderId != userId
            && (
                m.BorrowRequest.FromUserId == userId || m.BorrowRequest.ToUserId == userId
            )
        );

        return Ok(new UnreadCountResponse { Count = count });
    }

    [HttpGet("request/{borrowRequestId}")]
    public async Task<IActionResult> GetConversation(int borrowRequestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var borrowRequest = await _context
            .BorrowRequests.Include(r => r.Book)
            .Include(r => r.FromUser)
            .Include(r => r.ToUser)
            .FirstOrDefaultAsync(r => r.Id == borrowRequestId);

        if (borrowRequest == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        if (borrowRequest.FromUserId != userId && borrowRequest.ToUserId != userId)
        {
            return Forbid();
        }

        var messages = await _context
            .Messages.Where(m => m.BorrowRequestId == borrowRequestId)
            .Include(m => m.Sender)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Id)
            .ToListAsync();

        var unread = messages.Where(m => m.SenderId != userId && !m.IsRead).ToList();
        if (unread.Count > 0)
        {
            foreach (var message in unread)
            {
                message.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }

        var otherUser =
            borrowRequest.FromUserId == userId ? borrowRequest.ToUser : borrowRequest.FromUser;

        return Ok(
            new ConversationResponse
            {
                BookTitle = borrowRequest.Book.Title,
                OtherUserName = otherUser.DisplayName,
                Messages = messages
                    .Select(m => ToResponse(m, m.Sender.DisplayName))
                    .ToList(),
            }
        );
    }

    private static MessageResponse ToResponse(Message message, string senderName) =>
        new()
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = senderName,
            Text = message.Text,
            Date = message.Date,
        };
}

public class SendMessageRequest
{
    public int BorrowRequestId { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class MessageResponse
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class ConversationResponse
{
    public string BookTitle { get; set; } = string.Empty;
    public string OtherUserName { get; set; } = string.Empty;
    public List<MessageResponse> Messages { get; set; } = [];
}

public class UnreadCountResponse
{
    public int Count { get; set; }
}
