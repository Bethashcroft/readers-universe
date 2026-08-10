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
    public async Task TheSameBookAddedByTwoPeople_SharesOneCatalogueEntry()
    {
        var bethClient = _factory.CreateClient();
        var beth = await bethClient.RegisterAsync("beth");
        bethClient.Authenticate(beth.Token);
        var hers = await bethClient.AddBookAsync("The Hobbit", author: "J.R.R. Tolkien");

        var samClient = _factory.CreateClient();
        var sam = await samClient.RegisterAsync("sam");
        samClient.Authenticate(sam.Token);
        var his = await samClient.AddBookAsync("the   hobbit!", author: "JRR Tolkien");

        Assert.Equal(hers.BookId, his.BookId);
        Assert.NotEqual(hers.Id, his.Id);
    }

    [Fact]
    public async Task DifferentBooks_GetSeparateCatalogueEntries()
    {
        var user = await _client.RegisterAsync("reader");
        _client.Authenticate(user.Token);

        var hobbit = await _client.AddBookAsync("The Hobbit", author: "J.R.R. Tolkien");
        var dune = await _client.AddBookAsync("Dune", author: "Frank Herbert");

        Assert.NotEqual(hobbit.BookId, dune.BookId);
    }

    [Fact]
    public async Task AddingTheSameBookTwice_ReturnsBadRequest()
    {
        var user = await _client.RegisterAsync("reader");
        _client.Authenticate(user.Token);
        await _client.AddBookAsync("The Hobbit");

        var response = await _client.PostAsJsonAsync(
            "/api/library",
            new
            {
                title = "The Hobbit",
                author = "Test Author",
                coverUrl = "x",
                shelf = "read",
                offer = "none",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeletingSomeoneElsesBook_ReturnsForbiddenAndLeavesItIntact()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var entry = await ownerClient.AddBookAsync("Owner's Book");

        var intruder = await _client.RegisterAsync("intruder");
        _client.Authenticate(intruder.Token);

        var deleteResponse = await _client.DeleteAsync($"/api/library/{entry.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);

        var stillThere = await ownerClient.GetLibraryAsync();
        Assert.Contains(stillThere!, e => e.Id == entry.Id);
    }

    [Fact]
    public async Task UpdatingSomeoneElsesBook_ReturnsForbidden()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var entry = await ownerClient.AddBookAsync("Owner's Book");

        var intruder = await _client.RegisterAsync("intruder");
        _client.Authenticate(intruder.Token);

        var response = await _client.PutAsJsonAsync(
            $"/api/library/{entry.Id}",
            new { shelf = "read", offer = "for-sale" }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddingBookWithInvalidShelfOrOffer_ReturnsBadRequest()
    {
        var user = await _client.RegisterAsync("reader");
        _client.Authenticate(user.Token);

        var badShelf = await _client.PostAsJsonAsync(
            "/api/library",
            new
            {
                title = "Bad Shelf",
                author = "x",
                coverUrl = "x",
                shelf = "banana",
                offer = "none",
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, badShelf.StatusCode);

        var badOffer = await _client.PostAsJsonAsync(
            "/api/library",
            new
            {
                title = "Bad Offer",
                author = "x",
                coverUrl = "x",
                shelf = "read",
                offer = "banana",
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, badOffer.StatusCode);
    }

    [Fact]
    public async Task Browse_ShowsOfferedBooksRegardlessOfReadingShelf()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        await _client.AddBookAsync("Read and Selling", "read", "for-sale");
        await _client.AddBookAsync("Unread and Lending", "tbr", "available-to-borrow");
        await _client.AddBookAsync("Just Read", "read");

        var browse = await _client.GetBrowseAsync();
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
        var entry = await _client.AddBookAsync("Sold on Vinted", "read", "for-sale");

        var response = await _client.PutAsJsonAsync(
            $"/api/library/{entry.Id}",
            new { shelf = "read", offer = "none" }
        );
        response.EnsureSuccessStatusCode();

        var browse = await _client.GetBrowseAsync();
        Assert.DoesNotContain(browse!, b => b.Title == "Sold on Vinted");

        var mine = await _client.GetLibraryAsync();
        var after = mine!.Single(e => e.Id == entry.Id);
        Assert.Equal("read", after.Shelf);
        Assert.Equal("none", after.Offer);
    }

    [Fact]
    public async Task AddingAnExistingCatalogueBook_SharesItAndDoesNotDuplicate()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var theirs = await ownerClient.AddBookAsync("The Hobbit", author: "J.R.R. Tolkien");

        var reader = await _client.RegisterAsync("reader");
        _client.Authenticate(reader.Token);

        var response = await _client.PostAsJsonAsync(
            "/api/library",
            new
            {
                bookId = theirs.BookId,
                shelf = "tbr",
                offer = "none",
            }
        );
        response.EnsureSuccessStatusCode();
        var mine = await response.Content.ReadFromJsonAsync<BookResult>();

        Assert.Equal(theirs.BookId, mine!.BookId);
        Assert.Equal("The Hobbit", mine.Title);
        Assert.Equal("tbr", mine.Shelf);
    }

    [Fact]
    public async Task BookPage_ListsOtherOwnersOfferingTheBook()
    {
        var lenderClient = _factory.CreateClient();
        var lender = await lenderClient.RegisterAsync("lender");
        lenderClient.Authenticate(lender.Token);
        var theirs = await lenderClient.AddBookAsync(
            "The Hobbit",
            offer: "available-to-borrow"
        );

        var hoarderClient = _factory.CreateClient();
        var hoarder = await hoarderClient.RegisterAsync("hoarder");
        hoarderClient.Authenticate(hoarder.Token);
        await hoarderClient.PostAsJsonAsync(
            "/api/library",
            new
            {
                bookId = theirs.BookId,
                shelf = "read",
                offer = "none",
            }
        );

        var reader = await _client.RegisterAsync("reader");
        _client.Authenticate(reader.Token);

        var detail = await _client.GetFromJsonAsync<BookDetailResult>(
            $"/api/books/{theirs.BookId}"
        );

        Assert.Single(detail!.Owners);
        Assert.Equal("lender", detail.Owners[0].UserName);
        Assert.Equal("available-to-borrow", detail.Owners[0].Offer);
        Assert.Equal(theirs.Id, detail.Owners[0].LibraryEntryId);
    }

    [Fact]
    public async Task BookPage_DoesNotListYourselfAsAnOwner()
    {
        var user = await _client.RegisterAsync("reader");
        _client.Authenticate(user.Token);
        var mine = await _client.AddBookAsync("Mine", offer: "for-sale");

        var detail = await _client.GetFromJsonAsync<BookDetailResult>(
            $"/api/books/{mine.BookId}"
        );

        Assert.Empty(detail!.Owners);
        Assert.NotNull(detail.MyEntry);
    }

    [Fact]
    public async Task AddingABookWithARating_RecordsItAsAReview()
    {
        var user = await _client.RegisterAsync("reader");
        _client.Authenticate(user.Token);

        var entry = await _client.AddBookAsync("Rated On Add", rating: 5);

        Assert.Equal(5, entry.Rating);

        var detail = await _client.GetFromJsonAsync<BookDetailResult>(
            $"/api/books/{entry.BookId}"
        );
        Assert.Equal(5, detail!.AverageRating);
        Assert.Equal(1, detail.RatingCount);
    }
}
