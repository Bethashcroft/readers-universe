using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class BooksTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public BooksTests()
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
    public async Task DeletingSomeoneElsesBook_ReturnsForbiddenAndLeavesItIntact()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var book = await ownerClient.AddBookAsync("Owner's Book");

        var intruder = await _client.RegisterAsync("intruder");
        _client.Authenticate(intruder.Token);

        var deleteResponse = await _client.DeleteAsync($"/api/books/{book.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);

        var stillThere = await ownerClient.GetAsync($"/api/books/{book.Id}");
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
    }

    [Fact]
    public async Task UpdatingSomeoneElsesBook_ReturnsForbidden()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var book = await ownerClient.AddBookAsync("Owner's Book");

        var intruder = await _client.RegisterAsync("intruder");
        _client.Authenticate(intruder.Token);

        var response = await _client.PutAsJsonAsync(
            $"/api/books/{book.Id}",
            new
            {
                title = "Hijacked",
                author = "Intruder",
                coverUrl = "x",
                shelf = "read",
                offer = "for-sale",
                rating = (int?)null,
            }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Browse_ShowsOfferedBooksRegardlessOfReadingShelf()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        await _client.AddBookAsync("Read and Selling", "read", "for-sale");
        await _client.AddBookAsync("Unread and Lending", "tbr", "available-to-borrow");
        await _client.AddBookAsync("Just Read", "read");

        var browse = await _client.GetFromJsonAsync<BookResult[]>("/api/books/browse");
        var titles = browse!.Select(b => b.Title).ToArray();

        Assert.Contains("Read and Selling", titles);
        Assert.Contains("Unread and Lending", titles);
        Assert.DoesNotContain("Just Read", titles);
    }

    [Fact]
    public async Task ClearingTheOffer_RemovesBookFromBrowseButKeepsShelf()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        var book = await _client.AddBookAsync("Sold on Vinted", "read", "for-sale");

        var response = await _client.PutAsJsonAsync(
            $"/api/books/{book.Id}",
            new
            {
                title = book.Title,
                author = book.Author,
                coverUrl = book.CoverUrl,
                shelf = book.Shelf,
                offer = "none",
                rating = book.Rating,
            }
        );
        response.EnsureSuccessStatusCode();

        var browse = await _client.GetFromJsonAsync<BookResult[]>("/api/books/browse");
        Assert.DoesNotContain(browse!, b => b.Title == "Sold on Vinted");

        var after = await _client.GetFromJsonAsync<BookResult>($"/api/books/{book.Id}");
        Assert.Equal("read", after!.Shelf);
        Assert.Equal("none", after.Offer);
    }
}
