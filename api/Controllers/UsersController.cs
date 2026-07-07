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

        var query = _context.Books.Where(b => b.UserId == user.Id);

        if (user.Id != requesterId)
        {
            query = query.Where(b =>
                (b.Shelf != BookShelf.Tbr && b.Shelf != BookShelf.Dnf)
                || b.Offer != BookOffer.None
            );
        }

        var books = await query
            .Select(b => new BookResponse
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                CoverUrl = b.CoverUrl,
                Shelf = b.Shelf,
                Offer = b.Offer,
                Rating = b.Rating,
                UserId = b.UserId,
            })
            .ToListAsync();

        return Ok(books);
    }
}
