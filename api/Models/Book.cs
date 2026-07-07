using System.ComponentModel.DataAnnotations;

namespace ReadersRealm.Api.Models;

public class Book
{
    public int Id { get; set; }

    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Author { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string CoverUrl { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Shelf { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Offer { get; set; } = "none";
    public int? Rating { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
}
