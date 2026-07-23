using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ReadersRealm.Api.Models;

public class AppUser : IdentityUser
{
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Bio { get; set; } = string.Empty;

    [MaxLength(300)]
    public string VintedUrl { get; set; } = string.Empty;

    [MaxLength(300)]
    public string AvatarUrl { get; set; } = string.Empty;

    public byte[]? AvatarData { get; set; }

    [MaxLength(100)]
    public string AvatarContentType { get; set; } = string.Empty;

    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UsernameLastChangedAt { get; set; }
}
