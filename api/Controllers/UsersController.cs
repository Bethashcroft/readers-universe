using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public UsersController(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
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

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
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

        return Ok(ProfileResponse.FromUser(user));
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
