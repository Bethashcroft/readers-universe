using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class LendingLimitTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public LendingLimitTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static Task<HttpResponseMessage> OfferAsync(HttpClient client, int id) =>
        client.PostAsync($"/api/library/{id}/offer", null);

    private static Task<HttpResponseMessage> TakeBackAsync(HttpClient client, int id) =>
        client.DeleteAsync($"/api/library/{id}/offer");

    private static Task<HttpResponseMessage> SetOfferAsync(
        HttpClient client,
        int id,
        string offer,
        string shelf = "read"
    ) => client.PutAsJsonAsync($"/api/library/{id}", new { shelf, offer });

    private static async Task FillPoolAsync(HttpClient owner)
    {
        for (var i = 1; i <= 3; i++)
        {
            await owner.AddBookAsync($"Book {i}", offer: "available-to-borrow");
        }
    }

    [Fact]
    public async Task AddingAFourthOfferedBookIsRefused()
    {
        var owner = await _factory.SignInAsync("owner");
        await FillPoolAsync(owner);

        var refused = await owner.PostAsJsonAsync(
            "/api/library",
            new
            {
                title = "Book 4",
                author = "Test Author",
                coverUrl = "x",
                isbn = "",
                shelf = "read",
                offer = "available-to-borrow",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Contains("already have 3 books offered", await refused.ErrorMessageAsync());
        Assert.Equal(3, (await owner.GetBorrowingAsync()).Offering.Length);
    }

    [Fact]
    public async Task ChangingAFourthBookToOfferedIsRefused()
    {
        var owner = await _factory.SignInAsync("owner");
        await FillPoolAsync(owner);
        var fourth = await owner.AddBookAsync("Book 4");

        var refused = await SetOfferAsync(owner, fourth.Id, "available-to-borrow");

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    [Fact]
    public async Task OfferingAFourthBookIsRefused()
    {
        var owner = await _factory.SignInAsync("owner");
        await FillPoolAsync(owner);
        var fourth = await owner.AddBookAsync("Book 4");

        var refused = await OfferAsync(owner, fourth.Id);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal(3, (await owner.GetBorrowingAsync()).Offering.Length);
    }

    [Fact]
    public async Task LentOutCannotBePickedByHand()
    {
        var owner = await _factory.SignInAsync("owner");
        var book = await owner.AddBookAsync("Book 1");

        var viaUpdate = await SetOfferAsync(owner, book.Id, "lent-out");
        var viaAdd = await owner.PostAsJsonAsync(
            "/api/library",
            new
            {
                title = "Book 2",
                author = "Test Author",
                coverUrl = "x",
                isbn = "",
                shelf = "read",
                offer = "lent-out",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, viaUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, viaAdd.StatusCode);
    }

    [Fact]
    public async Task OfferingAnAlreadyOfferedBookIsHarmless()
    {
        var owner = await _factory.SignInAsync("owner");
        var book = await owner.AddBookAsync("Book 1", offer: "available-to-borrow");

        (await OfferAsync(owner, book.Id)).EnsureSuccessStatusCode();

        Assert.Single((await owner.GetBorrowingAsync()).Offering);
    }

    [Fact]
    public async Task LentOutBooksStillCountTowardTheLimit()
    {
        var owner = await _factory.SignInAsync("owner");
        var lent = await owner.AddBookAsync("Book 1", offer: "available-to-borrow");
        await owner.AddBookAsync("Book 2", offer: "available-to-borrow");
        await owner.AddBookAsync("Book 3", offer: "available-to-borrow");

        var borrower = await _factory.SignInAsync("borrower");
        await owner.TrustAsync("borrower");
        var request = await borrower.RequestBookAsync(lent.Id);
        await owner.AcceptRequestAsync(request.Id);

        var fourth = await owner.AddBookAsync("Book 4");
        var refused = await OfferAsync(owner, fourth.Id);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal(3, (await owner.GetBorrowingAsync()).Offering.Length);
    }

    [Fact]
    public async Task YouCannotOfferAWishlistBook()
    {
        var owner = await _factory.SignInAsync("owner");
        var wish = await owner.AddBookAsync("Someday", shelf: "want-to-read");

        var refused = await OfferAsync(owner, wish.Id);
        var viaUpdate = await SetOfferAsync(owner, wish.Id, "available-to-borrow", "want-to-read");

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal("You can only offer books you own.", await refused.ErrorMessageAsync());
        Assert.Equal(HttpStatusCode.BadRequest, viaUpdate.StatusCode);
    }

    [Fact]
    public async Task YouCannotOfferABookThatIsForSale()
    {
        var owner = await _factory.SignInAsync("owner");
        var sale = await owner.AddBookAsync("Selling", offer: "for-sale");

        var refused = await OfferAsync(owner, sale.Id);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal("for-sale", (await owner.GetLibraryAsync()).Single().Offer);
    }

    [Fact]
    public async Task YouCannotOfferSomeoneElsesBook()
    {
        var other = await _factory.SignInAsync("other");
        var theirs = await other.AddBookAsync("Not Mine");

        var owner = await _factory.SignInAsync("owner");
        var refused = await OfferAsync(owner, theirs.Id);

        Assert.Equal(HttpStatusCode.NotFound, refused.StatusCode);
    }

    [Fact]
    public async Task TakingABookBackDeclinesItsPendingRequests()
    {
        var owner = await _factory.SignInAsync("owner");
        var book = await owner.AddBookAsync("Book 1", offer: "available-to-borrow");

        var borrower = await _factory.SignInAsync("borrower");
        await owner.TrustAsync("borrower");
        var request = await borrower.RequestBookAsync(book.Id);

        (await TakeBackAsync(owner, book.Id)).EnsureSuccessStatusCode();

        Assert.Equal("none", (await owner.GetLibraryAsync()).Single().Offer);
        Assert.Empty((await owner.GetBorrowingAsync()).Incoming);
        Assert.Equal("declined", (await borrower.GetBorrowingAsync()).History.Single().Status);

        var accept = await owner.PutAsJsonAsync(
            $"/api/borrowrequests/{request.Id}",
            new { status = "accepted" }
        );
        Assert.Equal(HttpStatusCode.BadRequest, accept.StatusCode);
    }

    [Fact]
    public async Task ChangingTheOfferOnThePageAlsoDeclinesPendingRequests()
    {
        var owner = await _factory.SignInAsync("owner");
        var book = await owner.AddBookAsync("Book 1", offer: "available-to-borrow");

        var borrower = await _factory.SignInAsync("borrower");
        await owner.TrustAsync("borrower");
        await borrower.RequestBookAsync(book.Id);

        (await SetOfferAsync(owner, book.Id, "for-sale")).EnsureSuccessStatusCode();

        Assert.Empty((await owner.GetBorrowingAsync()).Incoming);
    }

    [Fact]
    public async Task ABookOnLoanCannotBeTakenBackChangedOrRemoved()
    {
        var owner = await _factory.SignInAsync("owner");
        var book = await owner.AddBookAsync("Book 1", offer: "available-to-borrow");

        var borrower = await _factory.SignInAsync("borrower");
        await owner.TrustAsync("borrower");
        var request = await borrower.RequestBookAsync(book.Id);
        await owner.AcceptRequestAsync(request.Id);

        Assert.Equal(HttpStatusCode.BadRequest, (await TakeBackAsync(owner, book.Id)).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await SetOfferAsync(owner, book.Id, "none")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await owner.DeleteAsync($"/api/library/{book.Id}")).StatusCode
        );

        (await SetOfferAsync(owner, book.Id, "lent-out", "currently-reading")).EnsureSuccessStatusCode();

        var entry = (await owner.GetLibraryAsync()).Single();
        Assert.Equal("lent-out", entry.Offer);
        Assert.Equal("currently-reading", entry.Shelf);
    }
}
