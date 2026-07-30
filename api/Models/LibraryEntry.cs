using System.ComponentModel.DataAnnotations;

namespace ReadersRealm.Api.Models;

public class LibraryEntry
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Shelf { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Offer { get; set; } = BookOffer.None;

    public DateTime AddedDate { get; set; } = DateTime.UtcNow;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
}
