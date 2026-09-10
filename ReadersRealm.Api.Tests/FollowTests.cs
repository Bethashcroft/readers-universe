using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record ProfileResult(
    string UserName,
    string DisplayName,
    bool IsPrivate,
    int FollowerCount,
    int FollowingCount,
    string FollowState,
    bool CanView
);

public record FollowRequestResult(string UserName, string DisplayName);

public record FollowListResult(string UserName, string DisplayName, string FollowState);

public class FollowTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public FollowTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> SignInAsync(string name)
    {
        var client = _factory.CreateClient();
        var user = await client.RegisterAsync(name);
        client.Authenticate(user.Token);
        return client;
    }

    private static async Task<ProfileResult> ProfileAsync(HttpClient client, string username) =>
        (await client.GetFromJsonAsync<ProfileResult>($"/api/users/{username}"))!;

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

    [Fact]
    public async Task FollowingAPublicAccountTakesEffectImmediately()
    {
        var rebel = await SignInAsync("rebel");
        var beth = await SignInAsync("beth");

        var follow = await beth.PostAsync("/api/users/rebel/follow", null);
        follow.EnsureSuccessStatusCode();

        Assert.Equal("following", (await ProfileAsync(beth, "rebel")).FollowState);
        Assert.Equal(1, (await ProfileAsync(rebel, "rebel")).FollowerCount);
        Assert.Equal(1, (await ProfileAsync(beth, "beth")).FollowingCount);
    }

    [Fact]
    public async Task FollowingAPrivateAccountWaitsForApproval()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");
        await beth.PostAsync("/api/users/rebel/follow", null);

        Assert.Equal("requested", (await ProfileAsync(beth, "rebel")).FollowState);
        Assert.Equal(0, (await ProfileAsync(rebel, "rebel")).FollowerCount);

        var pending = await rebel.GetFromJsonAsync<FollowRequestResult[]>(
            "/api/users/follow-requests"
        );
        Assert.Equal("beth", pending!.Single().UserName);

        (await rebel.PostAsync("/api/users/beth/approve-follow", null)).EnsureSuccessStatusCode();

        Assert.Equal("following", (await ProfileAsync(beth, "rebel")).FollowState);
        Assert.Equal(1, (await ProfileAsync(rebel, "rebel")).FollowerCount);
    }

    [Fact]
    public async Task DecliningRemovesTheRequestEntirely()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");
        await beth.PostAsync("/api/users/rebel/follow", null);
        (await rebel.PostAsync("/api/users/beth/decline-follow", null)).EnsureSuccessStatusCode();

        Assert.Equal("none", (await ProfileAsync(beth, "rebel")).FollowState);
        Assert.Empty(
            await rebel.GetFromJsonAsync<FollowRequestResult[]>("/api/users/follow-requests") ?? []
        );
    }

    [Fact]
    public async Task UnfollowingDropsTheFollowerCount()
    {
        var rebel = await SignInAsync("rebel");
        var beth = await SignInAsync("beth");

        await beth.PostAsync("/api/users/rebel/follow", null);
        await beth.DeleteAsync("/api/users/rebel/follow");

        Assert.Equal("none", (await ProfileAsync(beth, "rebel")).FollowState);
        Assert.Equal(0, (await ProfileAsync(rebel, "rebel")).FollowerCount);
    }

    [Fact]
    public async Task FollowingTwiceDoesNotDuplicate()
    {
        await SignInAsync("rebel");
        var beth = await SignInAsync("beth");

        await beth.PostAsync("/api/users/rebel/follow", null);
        await beth.PostAsync("/api/users/rebel/follow", null);

        Assert.Equal(1, (await ProfileAsync(beth, "rebel")).FollowerCount);
    }

    [Fact]
    public async Task APrivateLibraryIsHiddenFromStrangers()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");

        Assert.False((await ProfileAsync(beth, "rebel")).CanView);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await beth.GetAsync("/api/users/rebel/books")).StatusCode
        );
    }

    [Fact]
    public async Task APendingRequestDoesNotUnlockAPrivateLibrary()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");
        await beth.PostAsync("/api/users/rebel/follow", null);

        Assert.False((await ProfileAsync(beth, "rebel")).CanView);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await beth.GetAsync("/api/users/rebel/books")).StatusCode
        );
    }

    [Fact]
    public async Task AnApprovedFollowerCanSeeAPrivateLibrary()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");
        await beth.PostAsync("/api/users/rebel/follow", null);
        await rebel.PostAsync("/api/users/beth/approve-follow", null);

        Assert.True((await ProfileAsync(beth, "rebel")).CanView);
        (await beth.GetAsync("/api/users/rebel/books")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task YouCanAlwaysSeeYourOwnPrivateLibrary()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        Assert.True((await ProfileAsync(rebel, "rebel")).CanView);
        (await rebel.GetAsync("/api/users/rebel/books")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task TheFollowerListShowsWhoFollowsYou()
    {
        await SignInAsync("rebel");
        var beth = await SignInAsync("beth");

        await beth.PostAsync("/api/users/rebel/follow", null);

        var followers = await beth.GetFromJsonAsync<FollowListResult[]>(
            "/api/users/rebel/followers"
        );

        Assert.Equal("beth", followers!.Single().UserName);
        Assert.Equal("self", followers.Single().FollowState);
    }

    [Fact]
    public async Task TheFollowingListShowsWhoYouFollow()
    {
        await SignInAsync("rebel");
        var beth = await SignInAsync("beth");

        await beth.PostAsync("/api/users/rebel/follow", null);

        var following = await beth.GetFromJsonAsync<FollowListResult[]>(
            "/api/users/beth/following"
        );

        Assert.Equal("rebel", following!.Single().UserName);
        Assert.Equal("following", following.Single().FollowState);
    }

    [Fact]
    public async Task PendingRequestsAreNotInTheFollowerList()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");
        await beth.PostAsync("/api/users/rebel/follow", null);

        var followers = await rebel.GetFromJsonAsync<FollowListResult[]>(
            "/api/users/rebel/followers"
        );

        Assert.Empty(followers!);
    }

    [Fact]
    public async Task APrivateReadersFollowerListIsHidden()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await beth.GetAsync("/api/users/rebel/followers")).StatusCode
        );
    }

    [Fact]
    public async Task APrivateReadersBooksAreHiddenFromBrowse()
    {
        var rebel = await SignInAsync("rebel");
        await rebel.AddBookAsync("Piranesi", offer: "available-to-borrow");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");

        Assert.Empty(await beth.GetBrowseAsync());
    }

    [Fact]
    public async Task AnApprovedFollowerStillSeesPrivateBooksInBrowse()
    {
        var rebel = await SignInAsync("rebel");
        await rebel.AddBookAsync("Piranesi", offer: "available-to-borrow");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");
        await beth.PostAsync("/api/users/rebel/follow", null);
        await rebel.PostAsync("/api/users/beth/approve-follow", null);

        Assert.Equal("Piranesi", (await beth.GetBrowseAsync()).Single().Title);
    }

    [Fact]
    public async Task APrivateReaderIsNotListedAsABookOwner()
    {
        var rebel = await SignInAsync("rebel");
        var entry = await rebel.AddBookAsync("Piranesi", offer: "available-to-borrow");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");
        var detail = await beth.GetFromJsonAsync<BookDetailResult>(
            $"/api/books/{entry.BookId}"
        );

        Assert.Empty(detail!.Owners);
    }

    [Fact]
    public async Task YouCannotFollowYourself()
    {
        var beth = await SignInAsync("beth");

        var response = await beth.PostAsync("/api/users/beth/follow", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("self", (await ProfileAsync(beth, "beth")).FollowState);
    }
}
