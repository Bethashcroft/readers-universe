using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record CommentResult(
    int Id,
    string Text,
    DateTime? EditedDate,
    string UserName,
    string DisplayName,
    bool CanEdit,
    bool CanDelete
);

public class CommentTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public CommentTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static async Task<ActivityResult[]> FeedAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ActivityResult[]>("/api/feed"))!;

    private static Task<HttpResponseMessage> CommentAsync(
        HttpClient client,
        int activityId,
        string text
    ) => client.PostAsJsonAsync($"/api/feed/{activityId}/comments", new { text });

    private static async Task<CommentResult[]> GetCommentsAsync(
        HttpClient client,
        int activityId
    ) => (await client.GetFromJsonAsync<CommentResult[]>($"/api/feed/{activityId}/comments"))!;

    private static Task<HttpResponseMessage> DeleteCommentAsync(HttpClient client, int commentId) =>
        client.DeleteAsync($"/api/feed/comments/{commentId}");

    private static Task<HttpResponseMessage> EditCommentAsync(
        HttpClient client,
        int commentId,
        string text
    ) => client.PutAsJsonAsync($"/api/feed/comments/{commentId}", new { text });

    private static async Task<NotificationResult[]> NotificationsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<NotificationResult[]>("/api/notifications"))!;

    private async Task<(HttpClient Sophie, HttpClient Beth, int ActivityId)> SophieReadsAndBethFollowsAsync()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var beth = await _factory.SignInAsync("beth");
        await beth.PostAsync("/api/users/sophie/follow", null);
        await sophie.AddBookAsync("Babel", shelf: "read");

        var activity = (await FeedAsync(beth)).Single();
        return (sophie, beth, activity.Id);
    }

    [Fact]
    public async Task CommentingShowsUpAndTellsTheOwner()
    {
        var (sophie, beth, activityId) = await SophieReadsAndBethFollowsAsync();

        var response = await CommentAsync(beth, activityId, "  Loved this one  ");
        response.EnsureSuccessStatusCode();

        var comments = (await response.Content.ReadFromJsonAsync<CommentResult[]>())!;
        var only = Assert.Single(comments);
        Assert.Equal("Loved this one", only.Text);
        Assert.Equal("beth", only.UserName);

        Assert.Equal(1, (await FeedAsync(beth)).Single().CommentCount);

        var ping = Assert.Single(await NotificationsAsync(sophie), n => n.Type == "commented");
        Assert.Equal("Babel", ping.BookTitle);
    }

    [Fact]
    public async Task CommentsComeBackOldestFirst()
    {
        var (sophie, beth, activityId) = await SophieReadsAndBethFollowsAsync();

        await CommentAsync(beth, activityId, "First");
        await CommentAsync(sophie, activityId, "Second");

        var comments = await GetCommentsAsync(beth, activityId);

        Assert.Equal(["First", "Second"], comments.Select(c => c.Text));
    }

    [Fact]
    public async Task EmptyOrHugeCommentsAreRefused()
    {
        var (_, beth, activityId) = await SophieReadsAndBethFollowsAsync();

        var blank = await CommentAsync(beth, activityId, "   ");
        var huge = await CommentAsync(beth, activityId, new string('x', 501));

        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, huge.StatusCode);
        Assert.Equal("Write something first.", await blank.ErrorMessageAsync());
    }

    [Fact]
    public async Task YouCanEditYourOwnComment()
    {
        var (_, beth, activityId) = await SophieReadsAndBethFollowsAsync();
        await CommentAsync(beth, activityId, "Lvoed this");
        var mine = (await GetCommentsAsync(beth, activityId)).Single();

        Assert.True(mine.CanEdit);

        var response = await EditCommentAsync(beth, mine.Id, "  Loved this  ");
        response.EnsureSuccessStatusCode();

        var after = (await response.Content.ReadFromJsonAsync<CommentResult[]>())!.Single();
        Assert.Equal("Loved this", after.Text);
        Assert.Equal(mine.Id, after.Id);
        Assert.Null(mine.EditedDate);
        Assert.NotNull(after.EditedDate);
    }

    [Fact]
    public async Task SavingTheSameWordsIsNotAnEdit()
    {
        var (_, beth, activityId) = await SophieReadsAndBethFollowsAsync();
        await CommentAsync(beth, activityId, "No change here");
        var mine = (await GetCommentsAsync(beth, activityId)).Single();

        var response = await EditCommentAsync(beth, mine.Id, "  No change here  ");
        response.EnsureSuccessStatusCode();

        var after = (await response.Content.ReadFromJsonAsync<CommentResult[]>())!.Single();
        Assert.Null(after.EditedDate);
    }

    [Fact]
    public async Task TheOwnerCanDeleteACommentButNotRewriteIt()
    {
        var (sophie, beth, activityId) = await SophieReadsAndBethFollowsAsync();
        await CommentAsync(beth, activityId, "Mine to write");
        var hers = (await GetCommentsAsync(sophie, activityId)).Single();

        Assert.True(hers.CanDelete);
        Assert.False(hers.CanEdit);

        var response = await EditCommentAsync(sophie, hers.Id, "Words I never said");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "Mine to write",
            (await GetCommentsAsync(beth, activityId)).Single().Text
        );
    }

    [Fact]
    public async Task AnEditCannotBlankTheComment()
    {
        var (_, beth, activityId) = await SophieReadsAndBethFollowsAsync();
        await CommentAsync(beth, activityId, "Something");
        var mine = (await GetCommentsAsync(beth, activityId)).Single();

        var response = await EditCommentAsync(beth, mine.Id, "   ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Something", (await GetCommentsAsync(beth, activityId)).Single().Text);
    }

    [Fact]
    public async Task YouCanDeleteYourOwnComment()
    {
        var (_, beth, activityId) = await SophieReadsAndBethFollowsAsync();
        await CommentAsync(beth, activityId, "Oops");
        var mine = (await GetCommentsAsync(beth, activityId)).Single();

        Assert.True(mine.CanDelete);

        var response = await DeleteCommentAsync(beth, mine.Id);
        response.EnsureSuccessStatusCode();

        Assert.Empty(await GetCommentsAsync(beth, activityId));
    }

    [Fact]
    public async Task TheOwnerCanDeleteAnyCommentOnTheirUpdate()
    {
        var (sophie, beth, activityId) = await SophieReadsAndBethFollowsAsync();
        await CommentAsync(beth, activityId, "Rude thing");
        var hers = (await GetCommentsAsync(sophie, activityId)).Single();

        Assert.True(hers.CanDelete);

        (await DeleteCommentAsync(sophie, hers.Id)).EnsureSuccessStatusCode();

        Assert.Empty(await GetCommentsAsync(sophie, activityId));
    }

    [Fact]
    public async Task AStrangerCannotDeleteYourComment()
    {
        var (_, beth, activityId) = await SophieReadsAndBethFollowsAsync();
        await CommentAsync(beth, activityId, "Mine");
        var mine = (await GetCommentsAsync(beth, activityId)).Single();

        var tom = await _factory.SignInAsync("tom");
        await tom.PostAsync("/api/users/sophie/follow", null);

        var seenByTom = (await GetCommentsAsync(tom, activityId)).Single();
        Assert.False(seenByTom.CanDelete);

        var response = await DeleteCommentAsync(tom, mine.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Single(await GetCommentsAsync(beth, activityId));
    }

    [Fact]
    public async Task YouCannotCommentOnWhatYouCannotSee()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await sophie.PutAsJsonAsync(
            "/api/auth/profile",
            new
            {
                userName = "sophie",
                displayName = "sophie",
                bio = "",
                vintedUrl = "",
                isPrivate = true,
            }
        );
        await sophie.AddBookAsync("Babel", shelf: "read");
        var activity = (
            await sophie.GetFromJsonAsync<ActivityResult[]>("/api/users/sophie/activity")
        )!.Single();

        var tom = await _factory.SignInAsync("tom");
        var response = await CommentAsync(tom, activity.Id, "Hello");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
