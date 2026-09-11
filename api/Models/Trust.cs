namespace ReadersRealm.Api.Models;

public class Trust
{
    public int Id { get; set; }

    public string TrusterId { get; set; } = string.Empty;
    public AppUser Truster { get; set; } = null!;

    public string TrustedId { get; set; } = string.Empty;
    public AppUser Trusted { get; set; } = null!;

    public DateTime Date { get; set; } = DateTime.UtcNow;
}
