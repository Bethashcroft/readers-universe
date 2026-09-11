using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record TrustProfileResult(string UserName, bool Trusted, bool TrustsMe);

public record TrustedReaderResult(string UserName, string DisplayName);

public class TrustTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public TrustTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> SignInAsync(string name)
    {
        var client = _factory.CreateClient();
        var user = await client.RegisterAsync(name);
        client.Authenticate(user.Token);
        return client;
    }

    private static async Task<TrustProfileResult> ProfileAsync(HttpClient client, string username) =>
        (await client.GetFromJsonAsync<TrustProfileResult>($"/api/users/{username}"))!;

    private static async Task<TrustedReaderResult[]> TrustedAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<TrustedReaderResult[]>("/api/users/trusted"))!;

    [Fact]
    public async Task OnlyTrustedReadersCanRequestABook()
    {
        var owner = await SignInAsync("owner");
        var book = await owner.AddBookAsync("Piranesi", offer: "available-to-borrow");

        var stranger = await SignInAsync("stranger");

        var refused = await stranger.PostAsJsonAsync(
            "/api/borrowrequests",
            new { libraryEntryId = book.Id, message = "" }
        );

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);

        await owner.TrustAsync("stranger");

        await stranger.RequestBookAsync(book.Id);
    }

    [Fact]
    public async Task ABookThatIsNotOfferedCannotBeRequested()
    {
        var owner = await SignInAsync("owner");
        var book = await owner.AddBookAsync("Piranesi");

        var friend = await SignInAsync("friend");
        await owner.TrustAsync("friend");

        var refused = await friend.PostAsJsonAsync(
            "/api/borrowrequests",
            new { libraryEntryId = book.Id, message = "" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    [Fact]
    public async Task UntrustingDeclinesTheirPendingRequests()
    {
        var owner = await SignInAsync("owner");
        var book = await owner.AddBookAsync("Piranesi", offer: "available-to-borrow");

        var friend = await SignInAsync("friend");
        await owner.TrustAsync("friend");
        var request = await friend.RequestBookAsync(book.Id);

        (await owner.DeleteAsync("/api/users/friend/trust")).EnsureSuccessStatusCode();

        var requests = await friend.GetFromJsonAsync<BorrowResult[]>("/api/borrowrequests");

        Assert.Equal("declined", requests!.Single(r => r.Id == request.Id).Status);
    }

    [Fact]
    public async Task AnUntrustedReadersRequestCannotBeAccepted()
    {
        var owner = await SignInAsync("owner");
        var book = await owner.AddBookAsync("Piranesi", offer: "available-to-borrow");

        var friend = await SignInAsync("friend");
        await owner.TrustAsync("friend");
        var request = await friend.RequestBookAsync(book.Id);

        await owner.DeleteAsync("/api/users/friend/trust");

        var accept = await owner.PutAsJsonAsync(
            $"/api/borrowrequests/{request.Id}",
            new { status = "accepted" }
        );

        Assert.Equal(HttpStatusCode.Forbidden, accept.StatusCode);
    }

    [Fact]
    public async Task TrustIsOneWay()
    {
        var owner = await SignInAsync("owner");
        var friend = await SignInAsync("friend");

        await owner.TrustAsync("friend");

        Assert.True((await ProfileAsync(owner, "friend")).Trusted);
        Assert.False((await ProfileAsync(owner, "friend")).TrustsMe);
        Assert.False((await ProfileAsync(friend, "owner")).Trusted);
        Assert.True((await ProfileAsync(friend, "owner")).TrustsMe);
    }

    [Fact]
    public async Task TrustingSomeoneNotifiesThem()
    {
        var owner = await SignInAsync("owner");
        var friend = await SignInAsync("friend");

        await owner.TrustAsync("friend");

        var theirs = await friend.GetFromJsonAsync<NotificationResult[]>("/api/notifications");

        Assert.Equal("trusted", theirs!.Single().Type);
        Assert.Equal("owner", theirs.Single().ActorUserName);
    }

    [Fact]
    public async Task UntrustingRemovesThemAndTheirNotification()
    {
        var owner = await SignInAsync("owner");
        var friend = await SignInAsync("friend");

        await owner.TrustAsync("friend");
        (await owner.DeleteAsync("/api/users/friend/trust")).EnsureSuccessStatusCode();

        Assert.Empty(await TrustedAsync(owner));
        Assert.False((await ProfileAsync(owner, "friend")).Trusted);
        Assert.Empty(
            await friend.GetFromJsonAsync<NotificationResult[]>("/api/notifications") ?? []
        );
    }

    [Fact]
    public async Task TheTrustedListShowsWhoYouTrust()
    {
        var owner = await SignInAsync("owner");
        await SignInAsync("friend");
        await SignInAsync("other");

        await owner.TrustAsync("friend");

        Assert.Equal("friend", (await TrustedAsync(owner)).Single().UserName);
    }

    [Fact]
    public async Task TrustingTwiceDoesNotDuplicate()
    {
        var owner = await SignInAsync("owner");
        await SignInAsync("friend");

        await owner.TrustAsync("friend");
        await owner.TrustAsync("friend");

        Assert.Single(await TrustedAsync(owner));
    }

    [Fact]
    public async Task YouCannotTrustYourself()
    {
        var owner = await SignInAsync("owner");

        var response = await owner.PostAsync("/api/users/owner/trust", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BrowseSaysWhetherYouCanRequestEachBook()
    {
        var owner = await SignInAsync("owner");
        await owner.AddBookAsync("Piranesi", offer: "available-to-borrow");

        var friend = await SignInAsync("friend");

        Assert.False((await friend.GetBrowseAsync()).Single().CanRequest);

        await owner.TrustAsync("friend");

        Assert.True((await friend.GetBrowseAsync()).Single().CanRequest);
    }
}
