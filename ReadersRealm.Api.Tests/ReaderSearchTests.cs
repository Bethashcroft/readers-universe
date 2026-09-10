using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record ReaderResult(
    string UserName,
    string DisplayName,
    string Bio,
    int BookCount
);

public class ReaderSearchTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public ReaderSearchTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<Paged<ReaderResult>> SearchAsync(string query = "") =>
        (await _client.GetFromJsonAsync<Paged<ReaderResult>>($"/api/users/search{query}"))!;

    [Fact]
    public async Task SearchRequiresSigningIn()
    {
        var response = await _client.GetAsync("/api/users/search");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchFindsOtherReadersButNeverYourself()
    {
        var otherClient = _factory.CreateClient();
        var other = await otherClient.RegisterAsync("rebel");
        otherClient.Authenticate(other.Token);

        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        var all = await SearchAsync();

        Assert.Equal(1, all.Total);
        Assert.Equal("rebel", all.Items.Single().UserName);
    }

    [Fact]
    public async Task SearchMatchesUsernameAndDisplayNameCaseInsensitively()
    {
        var otherClient = _factory.CreateClient();
        var other = await otherClient.RegisterAsync("bookdragon");
        otherClient.Authenticate(other.Token);

        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        Assert.Equal(1, (await SearchAsync("?q=DRAGON")).Total);
        Assert.Equal(1, (await SearchAsync("?q=bookdrag")).Total);
        Assert.Equal(0, (await SearchAsync("?q=nobodyhere")).Total);
    }

    [Fact]
    public async Task SearchIgnoresAnAtSignInFrontOfAUsername()
    {
        var otherClient = _factory.CreateClient();
        var other = await otherClient.RegisterAsync("bookdragon");
        otherClient.Authenticate(other.Token);

        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        Assert.Equal(1, (await SearchAsync("?q=@bookdragon")).Total);
        Assert.Equal(1, (await SearchAsync("?q=@BOOKDRAG")).Total);
        Assert.Equal(0, (await SearchAsync("?q=@nobodyhere")).Total);
    }

    [Fact]
    public async Task ReaderBookCountHidesPrivateShelvesJustLikeTheProfileDoes()
    {
        var otherClient = _factory.CreateClient();
        var other = await otherClient.RegisterAsync("rebel");
        otherClient.Authenticate(other.Token);
        await otherClient.AddBookAsync("Public Read", "read");
        await otherClient.AddBookAsync("Secret Wishlist", "want-to-read");
        await otherClient.AddBookAsync("Unread but Lending", "tbr", "available-to-borrow");

        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        var found = (await SearchAsync("?q=rebel")).Items.Single();

        Assert.Equal(2, found.BookCount);
    }

    [Fact]
    public async Task SearchIsPagedAcrossEveryone()
    {
        foreach (var name in new[] { "anna", "bella", "cara" })
        {
            var c = _factory.CreateClient();
            var u = await c.RegisterAsync(name);
            c.Authenticate(u.Token);
        }

        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        var first = await SearchAsync("?pageSize=2&page=1");
        var second = await SearchAsync("?pageSize=2&page=2");

        Assert.Equal(3, first.Total);
        Assert.Equal(2, first.Items.Length);
        Assert.Single(second.Items);
        Assert.Empty(
            first.Items.Select(r => r.UserName).Intersect(second.Items.Select(r => r.UserName))
        );
    }
}
