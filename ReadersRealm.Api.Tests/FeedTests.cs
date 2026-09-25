using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record ActivityBookResult(int Id, string Title);

public record ActivityUserResult(string UserName, string DisplayName);

public record ActivityResult(
    int Id,
    string Type,
    int? Rating,
    int? Page,
    int? PageCount,
    string Format,
    int LikeCount,
    bool LikedByMe,
    int CommentCount,
    string UserName,
    ActivityBookResult? Book,
    ActivityUserResult? TargetUser
);

public class FeedTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public FeedTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static async Task<ActivityResult[]> FeedAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ActivityResult[]>("/api/feed"))!;

    private static async Task<ActivityResult[]> ActivityAsync(HttpClient client, string username) =>
        (await client.GetFromJsonAsync<ActivityResult[]>($"/api/users/{username}/activity"))!;

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

    private static Task MoveAsync(HttpClient client, int entryId, string shelf, string offer = "none") =>
        client.PutAsJsonAsync($"/api/library/{entryId}", new { shelf, offer });

    [Fact]
    public async Task TheFeedShowsWhatPeopleYouFollowDo()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var beth = await _factory.SignInAsync("beth");
        await beth.PostAsync("/api/users/sophie/follow", null);

        var book = await sophie.AddBookAsync("Piranesi", shelf: "currently-reading");
        await MoveAsync(sophie, book.Id, "read");

        var feed = await FeedAsync(beth);

        Assert.Equal(["finished", "started-reading"], feed.Select(a => a.Type));
        Assert.All(feed, a => Assert.Equal("sophie", a.UserName));
        Assert.Equal("Piranesi", feed[0].Book!.Title);
    }

    [Fact]
    public async Task TheFeedIsEmptyWhenYouFollowNobody()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var beth = await _factory.SignInAsync("beth");

        await sophie.AddBookAsync("Piranesi", shelf: "read");

        Assert.Empty(await FeedAsync(beth));
    }

    [Fact]
    public async Task YourOwnActivityIsNotInYourFeed()
    {
        var beth = await _factory.SignInAsync("beth");

        await beth.AddBookAsync("Piranesi", shelf: "read");

        Assert.Empty(await FeedAsync(beth));
        Assert.Single(await ActivityAsync(beth, "beth"));
    }

    [Fact]
    public async Task APrivateAccountOnlyReachesApprovedFollowers()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await GoPrivateAsync(sophie, "sophie");
        await sophie.AddBookAsync("Piranesi", shelf: "read");

        var beth = await _factory.SignInAsync("beth");
        await beth.PostAsync("/api/users/sophie/follow", null);

        Assert.Empty(await FeedAsync(beth));
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await beth.GetAsync("/api/users/sophie/activity")).StatusCode
        );

        await sophie.PostAsync("/api/users/beth/approve-follow", null);

        Assert.Contains(await FeedAsync(beth), a => a.Type == "finished");
        Assert.Contains(await ActivityAsync(beth, "sophie"), a => a.Type == "finished");
    }

    [Fact]
    public async Task EveryShelfMoveHasItsOwnEvent()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Piranesi", shelf: "want-to-read");

        await MoveAsync(sophie, book.Id, "tbr");
        await MoveAsync(sophie, book.Id, "currently-reading");
        await MoveAsync(sophie, book.Id, "dnf");

        var types = (await ActivityAsync(sophie, "sophie")).Select(a => a.Type);

        Assert.Equal(["did-not-finish", "started-reading", "wants-to-read"], types);
    }

    [Fact]
    public async Task SavingTheSameShelfAgainIsSilent()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Piranesi", shelf: "read");

        await MoveAsync(sophie, book.Id, "read");
        await MoveAsync(sophie, book.Id, "read", "for-sale");

        Assert.Single(await ActivityAsync(sophie, "sophie"));
    }

    [Fact]
    public async Task FinishingCarriesYourStars()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Piranesi", shelf: "currently-reading");
        await sophie.AddReviewAsync(book.BookId, 5, "Loved it");
        await MoveAsync(sophie, book.Id, "read");

        var finished = (await ActivityAsync(sophie, "sophie")).First(a => a.Type == "finished");
        var reviewed = (await ActivityAsync(sophie, "sophie")).First(a => a.Type == "reviewed");

        Assert.Equal(5, finished.Rating);
        Assert.Equal(5, reviewed.Rating);
    }

    [Fact]
    public async Task OfferingABookIsAnEvent()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Piranesi");

        await sophie.OfferBookAsync(book.Id);

        var offered = (await ActivityAsync(sophie, "sophie")).Single(a => a.Type == "offered");
        Assert.Equal("Piranesi", offered.Book!.Title);
    }

    [Fact]
    public async Task PostsShowTheFormatOnYourShelvesRightNow()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var beth = await _factory.SignInAsync("beth");
        await beth.PostAsync("/api/users/sophie/follow", null);
        var book = await sophie.AddBookAsync("Piranesi", shelf: "currently-reading");

        Assert.Equal("", (await FeedAsync(beth)).Single().Format);

        await sophie.PutAsJsonAsync(
            $"/api/library/{book.Id}",
            new { shelf = "currently-reading", offer = "none", format = "audiobook" }
        );

        Assert.Equal("audiobook", (await FeedAsync(beth)).Single().Format);
        Assert.Equal("audiobook", (await ActivityAsync(sophie, "sophie")).Single().Format);

        await sophie.DeleteAsync($"/api/library/{book.Id}");

        Assert.Equal("", (await FeedAsync(beth)).Single().Format);
    }

    [Fact]
    public async Task FollowingSomeoneIsAnEventOnceItIsReal()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await GoPrivateAsync(sophie, "sophie");
        var tom = await _factory.SignInAsync("tom");
        var beth = await _factory.SignInAsync("beth");

        await beth.PostAsync("/api/users/tom/follow", null);
        await beth.PostAsync("/api/users/sophie/follow", null);

        var before = await ActivityAsync(beth, "beth");
        Assert.Equal("tom", before.Single().TargetUser!.UserName);

        await sophie.PostAsync("/api/users/beth/approve-follow", null);

        var after = await ActivityAsync(beth, "beth");
        Assert.Equal(["sophie", "tom"], after.Select(a => a.TargetUser!.UserName));
    }
}
