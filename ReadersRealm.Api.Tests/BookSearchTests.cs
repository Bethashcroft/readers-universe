using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record BookSummaryResult(int Id, string Title, string Author, string CoverUrl);

public class BookSearchTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public BookSearchTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static async Task<Paged<BookSummaryResult>> SearchAsync(HttpClient client, string q) =>
        (
            await client.GetFromJsonAsync<Paged<BookSummaryResult>>(
                $"/api/books/search?q={Uri.EscapeDataString(q)}"
            )
        )!;

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
    public async Task SearchRequiresSigningIn()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/books/search?q=babel");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchMatchesTitleOrAuthorCaseInsensitively()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await sophie.AddBookAsync("Babel", author: "R.F. Kuang");
        await sophie.AddBookAsync("Piranesi", author: "Susanna Clarke");

        var byTitle = await SearchAsync(sophie, "BABEL");
        var byAuthor = await SearchAsync(sophie, "kuang");
        var nothing = await SearchAsync(sophie, "zzz");

        Assert.Equal("Babel", byTitle.Items.Single().Title);
        Assert.Equal("Babel", byAuthor.Items.Single().Title);
        Assert.Equal(0, nothing.Total);
    }

    [Fact]
    public async Task ABookOnAPrivateShelfStillShowsUpAsABook()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await GoPrivateAsync(sophie, "sophie");
        await sophie.AddBookAsync("Babel", author: "R.F. Kuang");

        var tom = await _factory.SignInAsync("tom");
        var found = await SearchAsync(tom, "babel");

        Assert.Equal("Babel", found.Items.Single().Title);
    }
}
