using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        UserManager<AppUser> userManager,
        IConfiguration config,
        IWebHostEnvironment env
    )
    {
        _userManager = userManager;
        _config = config;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var token = GenerateToken(user);
        return Ok(
            new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName!,
                DisplayName = user.DisplayName,
            }
        );
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var token = GenerateToken(user);
        return Ok(
            new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName!,
                DisplayName = user.DisplayName,
            }
        );
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(ProfileResponse.FromUser(user));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(request.VintedUrl) && !IsVintedUrl(request.VintedUrl))
        {
            return BadRequest(
                new
                {
                    message = "That doesn't look like a Vinted link. It should start with https:// and point to vinted, e.g. https://www.vinted.co.uk/member/...",
                }
            );
        }

        user.DisplayName = request.DisplayName;
        user.Bio = request.Bio;
        user.VintedUrl = request.VintedUrl;

        await _userManager.UpdateAsync(user);

        return Ok(ProfileResponse.FromUser(user));
    }

    [HttpPost("profile/avatar")]
    [Authorize]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null)
        {
            return NotFound();
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No image was uploaded" });
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            return BadRequest(new { message = "Image must be 2MB or smaller" });
        }

        var extensions = new Dictionary<string, string>
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
        };

        if (!extensions.TryGetValue(file.ContentType, out var extension))
        {
            return BadRequest(new { message = "Only JPG, PNG, or WebP images are allowed" });
        }

        var avatarsDir = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            "avatars"
        );
        Directory.CreateDirectory(avatarsDir);

        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var oldPath = Path.Combine(avatarsDir, Path.GetFileName(user.AvatarUrl));
            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Delete(oldPath);
            }
        }

        var fileName = $"{user.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{extension}";
        var filePath = Path.Combine(avatarsDir, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        user.AvatarUrl = $"/avatars/{fileName}";
        await _userManager.UpdateAsync(user);

        return Ok(ProfileResponse.FromUser(user));
    }

    private static readonly string[] VintedDomains =
    [
        "com",
        "co.uk",
        "fr",
        "de",
        "pl",
        "es",
        "it",
        "nl",
        "be",
        "at",
        "cz",
        "sk",
        "lt",
        "lu",
        "pt",
        "se",
        "dk",
        "fi",
        "hu",
        "ro",
        "gr",
        "hr",
        "ie",
        "us",
        "com.tr",
    ];

    private static bool IsVintedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www."))
        {
            host = host["www.".Length..];
        }

        if (!host.StartsWith("vinted."))
        {
            return false;
        }

        return VintedDomains.Contains(host["vinted.".Length..]);
    }

    private string GenerateToken(AppUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class RegisterRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class ProfileResponse
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string VintedUrl { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime JoinedDate { get; set; }

    public static ProfileResponse FromUser(AppUser user) =>
        new()
        {
            UserName = user.UserName!,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            VintedUrl = user.VintedUrl,
            AvatarUrl = user.AvatarUrl,
            JoinedDate = user.JoinedDate,
        };
}

public class UpdateProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string VintedUrl { get; set; } = string.Empty;
}
