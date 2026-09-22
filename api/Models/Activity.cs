namespace ReadersRealm.Api.Models;

public class Activity
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public string Type { get; set; } = string.Empty;

    public int? BookId { get; set; }
    public Book? Book { get; set; }

    public string? TargetUserId { get; set; }
    public AppUser? TargetUser { get; set; }

    public int? Rating { get; set; }
    public int? Page { get; set; }
    public int? PageCount { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public List<Like> Likes { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
}
