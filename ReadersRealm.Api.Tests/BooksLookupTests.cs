using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class BooksLookupTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public BooksLookupTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private record LookupResult(string Title, string Author, string CoverUrl);

    [Fact]
    public async Task LookingUpAKnownIsbn_ReturnsTheBookDetails()
    {
        var user = await _client.RegisterAsync("reader");
        _client.Authenticate(user.Token);

        var response = await _client.GetAsync($"/api/books/lookup/{FakeBookLookup.KnownIsbn}");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LookupResult>();
        Assert.Equal("The Hobbit", result!.Title);
        Assert.Equal("J.R.R. Tolkien", result.Author);
        Assert.False(string.IsNullOrWhiteSpace(result.CoverUrl));
    }

    [Fact]
    public async Task LookingUpAnUnknownIsbn_ReturnsNotFound()
    {
        var user = await _client.RegisterAsync("reader");
        _client.Authenticate(user.Token);

        var response = await _client.GetAsync($"/api/books/lookup/{FakeBookLookup.UnknownIsbn}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LookingUpWithoutLoggingIn_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync($"/api/books/lookup/{FakeBookLookup.KnownIsbn}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
