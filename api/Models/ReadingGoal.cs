namespace ReadersRealm.Api.Models;

public class ReadingGoal
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public int Year { get; set; }
    public int Target { get; set; }
}
