using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Data;

public static class LibraryVisibility
{
    public static IQueryable<LibraryEntry> VisibleTo(
        this IQueryable<LibraryEntry> query,
        AppDbContext context,
        string? userId
    ) =>
        query.Where(e =>
            !e.User.IsPrivate
            || e.UserId == userId
            || context.Follows.Any(f =>
                f.FollowerId == userId && f.FollowingId == e.UserId && f.Approved
            )
        );
}
