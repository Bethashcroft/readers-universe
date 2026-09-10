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
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly NotificationService _notifications;

    public UsersController(
        AppDbContext context,
        UserManager<AppUser> userManager,
        NotificationService notifications
    )
    {
        _context = context;
        _userManager = userManager;
        _notifications = notifications;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int? page,
        [FromQuery] int? pageSize
    )
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (currentPage, size) = PagedResult<ReaderResponse>.Normalise(page, pageSize);

        var query = _context.Users.Where(u => u.Id != me);

        var term = (q ?? string.Empty).Trim().TrimStart('@').ToLower();

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(u =>
                u.UserName!.ToLower().Contains(term) || u.DisplayName.ToLower().Contains(term)
            );
        }

        var total = await query.CountAsync();

        var readers = await query
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Id)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .Select(u => new ReaderResponse
            {
                UserName = u.UserName!,
                DisplayName = u.DisplayName,
                Bio = u.Bio,
                AvatarUrl = u.AvatarUrl,
                JoinedDate = u.JoinedDate,
                BookCount = _context.LibraryEntries.Count(e =>
                    e.UserId == u.Id
                    && (
                        (e.Shelf != BookShelf.Tbr
                            && e.Shelf != BookShelf.WantToRead
                            && e.Shelf != BookShelf.Dnf)
                        || e.Offer != BookOffer.None
                    )
                ),
            })
            .ToListAsync();

        return Ok(PagedResult<ReaderResponse>.From(readers, currentPage, size, total));
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> GetByUsername(string username)
    {
        var user = await _userManager.FindByNameAsync(username);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = ProfileResponse.FromUser(user);

        profile.FollowerCount = await _context.Follows.CountAsync(f =>
            f.FollowingId == user.Id && f.Approved
        );
        profile.FollowingCount = await _context.Follows.CountAsync(f =>
            f.FollowerId == user.Id && f.Approved
        );
        profile.FollowState = await FollowStateAsync(me, user.Id);
        profile.CanView = await CanViewLibraryAsync(user, me);

        return Ok(profile);
    }

    [HttpPost("{username}/follow")]
    public async Task<IActionResult> Follow(string username)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var them = await _userManager.FindByNameAsync(username);

        if (them == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (them.Id == me)
        {
            return BadRequest(new { message = "You cannot follow yourself." });
        }

        var existing = await _context.Follows.FirstOrDefaultAsync(f =>
            f.FollowerId == me && f.FollowingId == them.Id
        );

        if (existing == null)
        {
            var approved = !them.IsPrivate;

            _context.Follows.Add(
                new Follow
                {
                    FollowerId = me!,
                    FollowingId = them.Id,
                    Approved = approved,
                }
            );

            await _context.SaveChangesAsync();

            await _notifications.AddAsync(
                them.Id,
                me!,
                approved ? NotificationTypes.NewFollower : NotificationTypes.FollowRequested
            );
        }

        return Ok(new { state = await FollowStateAsync(me, them.Id) });
    }

    [HttpDelete("{username}/follow")]
    public async Task<IActionResult> Unfollow(string username)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var them = await _userManager.FindByNameAsync(username);

        if (them == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var existing = await _context.Follows.FirstOrDefaultAsync(f =>
            f.FollowerId == me && f.FollowingId == them.Id
        );

        if (existing != null)
        {
            _context.Follows.Remove(existing);

            var stale = await _context
                .Notifications.Where(n =>
                    n.UserId == them.Id
                    && n.ActorId == me
                    && (
                        n.Type == NotificationTypes.FollowRequested
                        || n.Type == NotificationTypes.NewFollower
                    )
                )
                .ToListAsync();

            _context.Notifications.RemoveRange(stale);

            await _context.SaveChangesAsync();
        }

        return Ok(new { state = FollowStates.None });
    }

    [HttpGet("{username}/followers")]
    public Task<IActionResult> Followers(string username) => FollowListAsync(username, true);

    [HttpGet("{username}/following")]
    public Task<IActionResult> Following(string username) => FollowListAsync(username, false);

    private async Task<IActionResult> FollowListAsync(string username, bool followers)
    {
        var user = await _userManager.FindByNameAsync(username);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!await CanViewLibraryAsync(user, me))
        {
            return StatusCode(403, new { message = "This account is private." });
        }

        var query = followers
            ? _context
                .Follows.Where(f => f.FollowingId == user.Id && f.Approved)
                .Select(f => f.Follower)
            : _context
                .Follows.Where(f => f.FollowerId == user.Id && f.Approved)
                .Select(f => f.Following);

        var readers = await query
            .OrderBy(u => u.DisplayName)
            .Select(u => new FollowListResponse
            {
                UserName = u.UserName!,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                FollowState = u.Id == me ? FollowStates.Self : FollowStates.None,
            })
            .ToListAsync();

        var mine = await _context
            .Follows.Where(f => f.FollowerId == me)
            .Select(f => new { f.Following.UserName, f.Approved })
            .ToListAsync();

        var states = mine.ToDictionary(f => f.UserName!, f => f.Approved);

        foreach (var reader in readers)
        {
            if (reader.FollowState == FollowStates.Self)
            {
                continue;
            }

            if (states.TryGetValue(reader.UserName, out var approved))
            {
                reader.FollowState = approved
                    ? FollowStates.Following
                    : FollowStates.Requested;
            }
        }

        return Ok(readers);
    }

    [HttpGet("follow-requests")]
    public async Task<IActionResult> FollowRequests()
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var requests = await _context
            .Follows.Where(f => f.FollowingId == me && !f.Approved)
            .OrderByDescending(f => f.RequestedDate)
            .Select(f => new FollowRequestResponse
            {
                UserName = f.Follower.UserName!,
                DisplayName = f.Follower.DisplayName,
                AvatarUrl = f.Follower.AvatarUrl,
                RequestedDate = f.RequestedDate,
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpPost("{username}/approve-follow")]
    public async Task<IActionResult> ApproveFollow(string username)
    {
        var request = await FindPendingRequestAsync(username);

        if (request == null)
        {
            return NotFound(new { message = "No follow request from that reader." });
        }

        request.Approved = true;
        await _context.SaveChangesAsync();

        await _notifications.AddAsync(
            request.FollowerId,
            request.FollowingId,
            NotificationTypes.FollowApproved
        );

        return Ok(new { approved = true });
    }

    [HttpPost("{username}/decline-follow")]
    public async Task<IActionResult> DeclineFollow(string username)
    {
        var request = await FindPendingRequestAsync(username);

        if (request == null)
        {
            return NotFound(new { message = "No follow request from that reader." });
        }

        _context.Follows.Remove(request);
        await _context.SaveChangesAsync();

        return Ok(new { approved = false });
    }

    private async Task<Follow?> FindPendingRequestAsync(string username)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var them = await _userManager.FindByNameAsync(username);

        if (them == null)
        {
            return null;
        }

        return await _context.Follows.FirstOrDefaultAsync(f =>
            f.FollowerId == them.Id && f.FollowingId == me && !f.Approved
        );
    }

    private async Task<bool> CanViewLibraryAsync(AppUser them, string? me)
    {
        if (!them.IsPrivate || them.Id == me)
        {
            return true;
        }

        return await _context.Follows.AnyAsync(f =>
            f.FollowerId == me && f.FollowingId == them.Id && f.Approved
        );
    }

    private async Task<string> FollowStateAsync(string? me, string themId)
    {
        if (me == null)
        {
            return FollowStates.None;
        }

        if (me == themId)
        {
            return FollowStates.Self;
        }

        var follow = await _context.Follows.FirstOrDefaultAsync(f =>
            f.FollowerId == me && f.FollowingId == themId
        );

        if (follow == null)
        {
            return FollowStates.None;
        }

        return follow.Approved ? FollowStates.Following : FollowStates.Requested;
    }

    [HttpGet("{username}/books")]
    public async Task<IActionResult> GetUserBooks(string username)
    {
        var user = await _userManager.FindByNameAsync(username);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!await CanViewLibraryAsync(user, requesterId))
        {
            return StatusCode(403, new { message = "This account is private." });
        }

        var query = _context.LibraryEntries.Where(e => e.UserId == user.Id);

        if (user.Id != requesterId)
        {
            query = query.Where(e =>
                (
                    e.Shelf != BookShelf.Tbr
                    && e.Shelf != BookShelf.WantToRead
                    && e.Shelf != BookShelf.Dnf
                )
                || e.Offer != BookOffer.None
            );
        }

        var entries = await query
            .Include(e => e.Book)
            .Include(e => e.User)
            .OrderByDescending(e => e.Id)
            .ToListAsync();

        return Ok(await LibraryEntryMapper.MapAsync(_context, entries, e => e.UserId));
    }
}

public class ReaderResponse
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime JoinedDate { get; set; }
    public int BookCount { get; set; }
}

public class FollowListResponse
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string FollowState { get; set; } = FollowStates.None;
}

public class FollowRequestResponse
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; }
}
