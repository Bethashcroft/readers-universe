using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
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

    public AuthController(UserManager<AppUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config = config;
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

        var newUserName = request.UserName?.Trim();
        if (
            !string.IsNullOrEmpty(newUserName)
            && !string.Equals(newUserName, user.UserName, StringComparison.OrdinalIgnoreCase)
        )
        {
            if (user.UsernameLastChangedAt is DateTime lastChanged)
            {
                var nextAllowed = lastChanged.AddDays(30);
                if (DateTime.UtcNow < nextAllowed)
                {
                    return BadRequest(
                        new
                        {
                            message = $"You can only change your username once every 30 days. You can change it again on {nextAllowed:d MMMM yyyy}.",
                        }
                    );
                }
            }

            if (!UsernameRegex.IsMatch(newUserName))
            {
                return BadRequest(
                    new
                    {
                        message = "Username must be 5–20 characters, using only letters, numbers, dots and underscores.",
                    }
                );
            }

            var existing = await _userManager.FindByNameAsync(newUserName);
            if (existing != null && existing.Id != user.Id)
            {
                return BadRequest(new { message = "That username is already taken." });
            }

            user.UserName = newUserName;
            user.NormalizedUserName = _userManager.NormalizeName(newUserName);
            user.UsernameLastChangedAt = DateTime.UtcNow;
        }

        user.DisplayName = request.DisplayName;
        user.Bio = request.Bio;
        user.VintedUrl = request.VintedUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

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

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Only JPG, PNG, or WebP images are allowed" });
        }

        using var memory = new MemoryStream();
        await file.CopyToAsync(memory);

        user.AvatarData = memory.ToArray();
        user.AvatarContentType = file.ContentType;
        var version = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        user.AvatarUrl = $"/api/avatars/{user.Id}?v={version}";
        await _userManager.UpdateAsync(user);

        return Ok(ProfileResponse.FromUser(user));
    }

    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9._]{5,20}$");

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
    public DateTime? UsernameChangeableOn { get; set; }

    public static ProfileResponse FromUser(AppUser user) =>
        new()
        {
            UserName = user.UserName!,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            VintedUrl = user.VintedUrl,
            AvatarUrl = user.AvatarUrl,
            JoinedDate = user.JoinedDate,
            UsernameChangeableOn = user.UsernameLastChangedAt?.AddDays(30),
        };
}

public class UpdateProfileRequest
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string VintedUrl { get; set; } = string.Empty;
}
