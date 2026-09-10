namespace ReadersRealm.Api.Models;

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public string ActorId { get; set; } = string.Empty;
    public AppUser Actor { get; set; } = null!;

    public string Type { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;
}
