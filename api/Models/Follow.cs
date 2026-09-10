namespace ReadersRealm.Api.Models;

public class Follow
{
    public int Id { get; set; }

    public string FollowerId { get; set; } = string.Empty;
    public AppUser Follower { get; set; } = null!;

    public string FollowingId { get; set; } = string.Empty;
    public AppUser Following { get; set; } = null!;

    public bool Approved { get; set; }

    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
}
