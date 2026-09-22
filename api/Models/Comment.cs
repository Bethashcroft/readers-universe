namespace ReadersRealm.Api.Models;

public class Comment
{
    public int Id { get; set; }

    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public string Text { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public DateTime? EditedDate { get; set; }
}
