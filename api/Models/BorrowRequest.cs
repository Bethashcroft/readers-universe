using System.ComponentModel.DataAnnotations;

namespace ReadersRealm.Api.Models;

public class BorrowRequest
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = BorrowStatus.Pending;

    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public string FromUserId { get; set; } = string.Empty;
    public AppUser FromUser { get; set; } = null!;

    public string ToUserId { get; set; } = string.Empty;
    public AppUser ToUser { get; set; } = null!;
}