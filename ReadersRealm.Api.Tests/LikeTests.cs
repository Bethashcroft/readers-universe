using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record LikesResult(int LikeCount, bool LikedByMe);

public class LikeTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public LikeTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static async Task<ActivityResult[]> FeedAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ActivityResult[]>("/api/feed"))!;

    private static Task<HttpResponseMessage> LikeAsync(HttpClient client, int activityId) =>
        client.PostAsync($"/api/feed/{activityId}/like", null);

    private static Task<HttpResponseMessage> UnlikeAsync(HttpClient client, int activityId) =>
        client.DeleteAsync($"/api/feed/{activityId}/like");

    private static async Task<NotificationResult[]> NotificationsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<NotificationResult[]>("/api/notifications"))!;

    private static Task GoPrivateAsync(HttpClient client, string username) =>
        client.PutAsJsonAsync(
            "/api/auth/profile",
            new
            {
                userName = username,
                displayName = username,
                bio = "",
                vintedUrl = "",
                isPrivate = true,
            }
        );

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
    public async Task LikingAnUpdateCountsItAndTellsTheOwner()
    {
        var (sophie, beth, activityId) = await SophieReadsAndBethFollowsAsync();

        var response = await LikeAsync(beth, activityId);
        response.EnsureSuccessStatusCode();

        var likes = (await response.Content.ReadFromJsonAsync<LikesResult>())!;
        Assert.Equal(1, likes.LikeCount);
        Assert.True(likes.LikedByMe);

        var inFeed = (await FeedAsync(beth)).Single();
        Assert.Equal(1, inFeed.LikeCount);
        Assert.True(inFeed.LikedByMe);

        var ping = Assert.Single(await NotificationsAsync(sophie), n => n.Type == "liked");
        Assert.Equal("beth", ping.ActorUserName);
        Assert.Equal("Babel", ping.BookTitle);
    }

    [Fact]
    public async Task LikingTwiceOnlyCountsOnce()
    {
        var (_, beth, activityId) = await SophieReadsAndBethFollowsAsync();

        await LikeAsync(beth, activityId);
        var second = await LikeAsync(beth, activityId);

        var likes = (await second.Content.ReadFromJsonAsync<LikesResult>())!;
        Assert.Equal(1, likes.LikeCount);
    }

    [Fact]
    public async Task UnlikingTakesItBack()
    {
        var (_, beth, activityId) = await SophieReadsAndBethFollowsAsync();
        await LikeAsync(beth, activityId);

        var response = await UnlikeAsync(beth, activityId);
        response.EnsureSuccessStatusCode();

        var likes = (await response.Content.ReadFromJsonAsync<LikesResult>())!;
        Assert.Equal(0, likes.LikeCount);
        Assert.False(likes.LikedByMe);
    }

    [Fact]
    public async Task OtherReadersSeeTheCountButNotYourHeart()
    {
        var (_, beth, activityId) = await SophieReadsAndBethFollowsAsync();
        await LikeAsync(beth, activityId);

        var tom = await _factory.SignInAsync("tom");
        await tom.PostAsync("/api/users/sophie/follow", null);

        var seenByTom = (await FeedAsync(tom)).Single();
        Assert.Equal(1, seenByTom.LikeCount);
        Assert.False(seenByTom.LikedByMe);
    }

    [Fact]
    public async Task LikingYourOwnUpdateDoesNotNotifyYou()
    {
        var (sophie, _, activityId) = await SophieReadsAndBethFollowsAsync();

        await LikeAsync(sophie, activityId);

        Assert.DoesNotContain(await NotificationsAsync(sophie), n => n.Type == "liked");
    }

    [Fact]
    public async Task YouCannotLikeWhatYouCannotSee()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await GoPrivateAsync(sophie, "sophie");
        await sophie.AddBookAsync("Babel", shelf: "read");
        var activity = (
            await sophie.GetFromJsonAsync<ActivityResult[]>("/api/users/sophie/activity")
        )!.Single();

        var tom = await _factory.SignInAsync("tom");
        var response = await LikeAsync(tom, activity.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
