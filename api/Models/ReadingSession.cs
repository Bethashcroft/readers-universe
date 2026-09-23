namespace ReadersRealm.Api.Models;

public class ReadingSession
{
    public int Id { get; set; }

    public int LibraryEntryId { get; set; }
    public LibraryEntry LibraryEntry { get; set; } = null!;

    public DateTime FinishedDate { get; set; }
}
