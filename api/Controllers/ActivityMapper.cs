using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

public static class ActivityMapper
{
    public const int PageLength = 30;

    public static IQueryable<ActivityResponse> ToResponses(
        this IQueryable<Activity> activities
    ) =>
        activities.Select(a => new ActivityResponse
        {
            Id = a.Id,
            Type = a.Type,
            Date = a.Date,
            Rating = a.Rating,
            UserName = a.User.UserName!,
            DisplayName = a.User.DisplayName,
            AvatarUrl = a.User.AvatarUrl,
            Book =
                a.Book == null
                    ? null
                    : new ActivityBookResponse
                    {
                        Id = a.Book.Id,
                        Title = a.Book.Title,
                        Author = a.Book.Author,
                        CoverUrl = a.Book.CoverUrl,
                    },
            TargetUser =
                a.TargetUser == null
                    ? null
                    : new ActivityUserResponse
                    {
                        UserName = a.TargetUser.UserName!,
                        DisplayName = a.TargetUser.DisplayName,
                    },
        });
}

public class ActivityResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int? Rating { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public ActivityBookResponse? Book { get; set; }
    public ActivityUserResponse? TargetUser { get; set; }
}

public class ActivityBookResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
}

public class ActivityUserResponse
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
