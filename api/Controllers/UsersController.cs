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
                (e.Shelf != BookShelf.Tbr && e.Shelf != BookShelf.Dnf)
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
