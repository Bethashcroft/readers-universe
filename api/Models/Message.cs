using System.ComponentModel.DataAnnotations;

namespace ReadersRealm.Api.Models;

public class Message
{
    public int Id { get; set; }

    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    public int BorrowRequestId { get; set; }
    public BorrowRequest BorrowRequest { get; set; } = null!;

    public string SenderId { get; set; } = string.Empty;
    public AppUser Sender { get; set; } = null!;
}
