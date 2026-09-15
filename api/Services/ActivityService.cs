using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public class ActivityService
{
    private readonly AppDbContext _context;

    public ActivityService(AppDbContext context)
    {
        _context = context;
    }

    public void Record(
        string userId,
        string type,
        int? bookId = null,
        string? targetUserId = null,
        int? rating = null
    )
    {
        _context.Activities.Add(
            new Activity
            {
                UserId = userId,
                Type = type,
                BookId = bookId,
                TargetUserId = targetUserId,
                Rating = rating,
            }
        );
    }

    public static string? ForShelf(string shelf) =>
        shelf switch
        {
            BookShelf.CurrentlyReading => ActivityTypes.StartedReading,
            BookShelf.Read => ActivityTypes.Finished,
            BookShelf.Dnf => ActivityTypes.DidNotFinish,
            BookShelf.WantToRead => ActivityTypes.WantsToRead,
            _ => null,
        };
}
