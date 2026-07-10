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
    public async Task AddingBookWithInvalidShelfOrOffer_ReturnsBadRequest()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);

        var badShelf = await _client.PostAsJsonAsync(
            "/api/books",
            new
            {
                title = "Bad Shelf",
                author = "x",
                coverUrl = "x",
                shelf = "banana",
                offer = "none",
                rating = (int?)null,
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, badShelf.StatusCode);

        var badOffer = await _client.PostAsJsonAsync(
            "/api/books",
            new
            {
                title = "Bad Offer",
                author = "x",
                coverUrl = "x",
                shelf = "read",
                offer = "banana",
                rating = (int?)null,
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, badOffer.StatusCode);
    }

    [Fact]
    public async Task FetchingSomeoneElsesPrivateShelfBook_ReturnsNotFound()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var privateBook = await ownerClient.AddBookAsync("Secret Wishlist", "tbr");
        var offeredBook = await ownerClient.AddBookAsync("Public Wishlist", "tbr", "for-sale");

        var viewer = await _client.RegisterAsync("viewer");
        _client.Authenticate(viewer.Token);

        var hidden = await _client.GetAsync($"/api/books/{privateBook.Id}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var visible = await _client.GetAsync($"/api/books/{offeredBook.Id}");
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);

        var own = await ownerClient.GetAsync($"/api/books/{privateBook.Id}");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
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
        Assert.All(browse!, b => Assert.Equal("owner", b.OwnerName));
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
