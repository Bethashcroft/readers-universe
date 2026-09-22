namespace ReadersRealm.Api.Models;

public class Like
{
    public int Id { get; set; }

    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public DateTime Date { get; set; } = DateTime.UtcNow;
}
