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
public class FeedController : ControllerBase
{
    public const int CommentLength = 500;

    private readonly AppDbContext _context;
    private readonly ReaderAccess _access;
    private readonly NotificationService _notifications;

    public FeedController(
        AppDbContext context,
        ReaderAccess access,
        NotificationService notifications
    )
    {
        _context = context;
        _access = access;
        _notifications = notifications;
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
            .ToResponses(me, _context.LibraryEntries)
            .ToListAsync();

        return Ok(feed);
    }

    [HttpPost("{id}/like")]
    public async Task<IActionResult> Like(int id)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var activity = await VisibleActivityAsync(id, me);

        if (activity == null)
        {
            return NotFound(new { message = "That update is not here any more." });
        }

        var already = await _context.Likes.AnyAsync(l => l.ActivityId == id && l.UserId == me);

        if (!already)
        {
            _context.Likes.Add(new Like { ActivityId = id, UserId = me });

            if (await _context.TrySaveChangesAsync())
            {
                await _notifications.AddAsync(activity.UserId, me, NotificationTypes.Liked, id);
            }
        }

        return Ok(await LikesAsync(id, me));
    }

    [HttpDelete("{id}/like")]
    public async Task<IActionResult> Unlike(int id)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var activity = await VisibleActivityAsync(id, me);

        if (activity == null)
        {
            return NotFound(new { message = "That update is not here any more." });
        }

        var like = await _context.Likes.FirstOrDefaultAsync(l =>
            l.ActivityId == id && l.UserId == me
        );

        if (like != null)
        {
            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();
        }

        return Ok(await LikesAsync(id, me));
    }

    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(int id)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var activity = await VisibleActivityAsync(id, me);

        if (activity == null)
        {
            return NotFound(new { message = "That update is not here any more." });
        }

        return Ok(await CommentsAsync(id, me, activity.UserId));
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest request)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var activity = await VisibleActivityAsync(id, me);

        if (activity == null)
        {
            return NotFound(new { message = "That update is not here any more." });
        }

        var text = (request.Text ?? string.Empty).Trim();
        var problem = ProblemWith(text);

        if (problem != null)
        {
            return BadRequest(new { message = problem });
        }

        _context.Comments.Add(
            new Comment
            {
                ActivityId = id,
                UserId = me,
                Text = text,
            }
        );

        await _context.SaveChangesAsync();
        await _notifications.AddAsync(activity.UserId, me, NotificationTypes.Commented, id);

        return Ok(await CommentsAsync(id, me, activity.UserId));
    }

    [HttpPut("comments/{commentId}")]
    public async Task<IActionResult> EditComment(
        int commentId,
        [FromBody] AddCommentRequest request
    )
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var comment = await _context
            .Comments.Include(c => c.Activity)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
        {
            return NotFound(new { message = "That comment has gone." });
        }

        if (comment.UserId != me)
        {
            return StatusCode(403, new { message = "That comment is not yours to edit." });
        }

        var text = (request.Text ?? string.Empty).Trim();
        var problem = ProblemWith(text);

        if (problem != null)
        {
            return BadRequest(new { message = problem });
        }

        if (text == comment.Text)
        {
            return Ok(await CommentsAsync(comment.ActivityId, me, comment.Activity.UserId));
        }

        comment.Text = text;
        comment.EditedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(await CommentsAsync(comment.ActivityId, me, comment.Activity.UserId));
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var comment = await _context
            .Comments.Include(c => c.Activity)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
        {
            return NotFound(new { message = "That comment has gone." });
        }

        if (comment.UserId != me && comment.Activity.UserId != me)
        {
            return StatusCode(403, new { message = "That comment is not yours to delete." });
        }

        var activityId = comment.ActivityId;
        var ownerId = comment.Activity.UserId;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        return Ok(await CommentsAsync(activityId, me, ownerId));
    }

    private static string? ProblemWith(string text) =>
        text.Length switch
        {
            0 => "Write something first.",
            > CommentLength => $"Comments can be up to {CommentLength} characters.",
            _ => null,
        };

    private Task<List<CommentResponse>> CommentsAsync(int activityId, string me, string ownerId) =>
        _context
            .Comments.Where(c => c.ActivityId == activityId)
            .OrderBy(c => c.Date)
            .ThenBy(c => c.Id)
            .Select(c => new CommentResponse
            {
                Id = c.Id,
                Text = c.Text,
                Date = c.Date,
                EditedDate = c.EditedDate,
                UserName = c.User.UserName!,
                DisplayName = c.User.DisplayName,
                AvatarUrl = c.User.AvatarUrl,
                CanEdit = c.UserId == me,
                CanDelete = c.UserId == me || ownerId == me,
            })
            .ToListAsync();

    private async Task<Activity?> VisibleActivityAsync(int id, string me)
    {
        var activity = await _context
            .Activities.Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activity == null || !await _access.CanViewLibraryAsync(activity.User, me))
        {
            return null;
        }

        return activity;
    }

    private async Task<LikesResponse> LikesAsync(int activityId, string me)
    {
        var likes = _context.Likes.Where(l => l.ActivityId == activityId);

        return new LikesResponse
        {
            LikeCount = await likes.CountAsync(),
            LikedByMe = await likes.AnyAsync(l => l.UserId == me),
        };
    }
}

public class LikesResponse
{
    public int LikeCount { get; set; }
    public bool LikedByMe { get; set; }
}

public class AddCommentRequest
{
    public string? Text { get; set; }
}

public class CommentResponse
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? EditedDate { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
