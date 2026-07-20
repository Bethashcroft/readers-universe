using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvatarsController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public AvatarsController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvatar(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user?.AvatarData == null || user.AvatarData.Length == 0)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=86400";
        return File(user.AvatarData, user.AvatarContentType);
    }
}
