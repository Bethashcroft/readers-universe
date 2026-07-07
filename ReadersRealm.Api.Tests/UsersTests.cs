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
    public async Task ViewingAnotherUsersProfile_HidesPrivateShelves()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        await _client.AddBookAsync("Public Read", "read");
        await _client.AddBookAsync("Secret Wishlist", "tbr");
        await _client.AddBookAsync("Abandoned", "dnf");

        var viewerClient = _factory.CreateClient();
        var viewer = await viewerClient.RegisterAsync("viewer");
        viewerClient.Authenticate(viewer.Token);

        var theirBooks = await viewerClient.GetFromJsonAsync<BookResult[]>(
            "/api/users/owner/books"
        );
        var titles = theirBooks!.Select(b => b.Title).ToArray();

        Assert.Contains("Public Read", titles);
        Assert.DoesNotContain("Secret Wishlist", titles);
        Assert.DoesNotContain("Abandoned", titles);
    }

    [Fact]
    public async Task ViewingAnotherUsersProfile_ShowsPrivateShelfBooksWithActiveOffers()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        await _client.AddBookAsync("Unread but Selling", "tbr", "for-sale");

        var viewerClient = _factory.CreateClient();
        var viewer = await viewerClient.RegisterAsync("viewer");
        viewerClient.Authenticate(viewer.Token);

        var theirBooks = await viewerClient.GetFromJsonAsync<BookResult[]>(
            "/api/users/owner/books"
        );

        Assert.Contains(theirBooks!, b => b.Title == "Unread but Selling");
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
