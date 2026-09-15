using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class UsersTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public UsersTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task ViewingAnotherUsersProfile_ShowsEveryShelf()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        await _client.AddBookAsync("Public Read", "read");
        await _client.AddBookAsync("Secret Wishlist", "tbr");
        await _client.AddBookAsync("Books I Fancy", "want-to-read");
        await _client.AddBookAsync("Abandoned", "dnf");

        var viewerClient = _factory.CreateClient();
        var viewer = await viewerClient.RegisterAsync("viewer");
        viewerClient.Authenticate(viewer.Token);

        var theirBooks = await viewerClient.GetFromJsonAsync<BookResult[]>(
            "/api/users/owner/books"
        );
        var titles = theirBooks!.Select(b => b.Title).ToArray();

        Assert.Contains("Public Read", titles);
        Assert.Contains("Secret Wishlist", titles);
        Assert.Contains("Books I Fancy", titles);
        Assert.Contains("Abandoned", titles);
    }

    [Fact]
    public async Task ViewingYourOwnProfile_ShowsAllShelves()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        await _client.AddBookAsync("Public Read", "read");
        await _client.AddBookAsync("Secret Wishlist", "tbr");

        var ownBooks = await _client.GetFromJsonAsync<BookResult[]>(
            "/api/users/owner/books"
        );
        var titles = ownBooks!.Select(b => b.Title).ToArray();

        Assert.Contains("Public Read", titles);
        Assert.Contains("Secret Wishlist", titles);
    }
}
