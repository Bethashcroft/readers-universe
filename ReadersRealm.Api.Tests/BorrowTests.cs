using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class BorrowTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public BorrowTests()
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
    public async Task RequestingYourOwnBook_ReturnsBadRequest()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);

        var book = await _client.AddBookAsync("My Own Book");

        var response = await _client.PostAsJsonAsync(
            "/api/borrowrequests",
            new { libraryEntryId = book.Id, message = "lend me my own book" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Equal("You cannot request your own book", body!.Message);
    }

    private record MessageResult(string Message);

    [Fact]
    public async Task RequestingABookYouHaveAlreadyRated_ReturnsBadRequest()
    {
        var lenderClient = _factory.CreateClient();
        var lender = await lenderClient.RegisterAsync("lender");
        lenderClient.Authenticate(lender.Token);
        var theirs = await lenderClient.AddBookAsync(
            "The Hobbit",
            offer: "available-to-borrow"
        );

        var reader = await _client.RegisterAsync("reader");
        _client.Authenticate(reader.Token);
        await _client.AddReviewAsync(theirs.BookId, 5, "Read it years ago");

        var response = await _client.PostAsJsonAsync(
            "/api/borrowrequests",
            new { libraryEntryId = theirs.Id, message = "fancy a reread" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Equal(
            "You've already rated this book, so you can't request it.",
            body!.Message
        );
    }

    [Fact]
    public async Task RequestingABookYouAlreadyHaveOnYourShelves_ReturnsBadRequest()
    {
        var lenderClient = _factory.CreateClient();
        var lender = await lenderClient.RegisterAsync("lender");
        lenderClient.Authenticate(lender.Token);
        var theirs = await lenderClient.AddBookAsync(
            "The Hobbit",
            offer: "available-to-borrow"
        );

        var reader = await _client.RegisterAsync("reader");
        _client.Authenticate(reader.Token);
        await _client.PostAsJsonAsync(
            "/api/library",
            new
            {
                bookId = theirs.BookId,
                shelf = "read",
                offer = "none",
            }
        );

        var response = await _client.PostAsJsonAsync(
            "/api/borrowrequests",
            new { libraryEntryId = theirs.Id, message = "can I borrow it" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Equal("This book is already on your shelves.", body!.Message);
    }

    [Fact]
    public async Task AcceptingRequest_DeclinesCompetitorsAndLendsOutTheBook()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var book = await ownerClient.AddBookAsync("Shared Book");

        var aliceClient = _factory.CreateClient();
        var alice = await aliceClient.RegisterAsync("alice");
        aliceClient.Authenticate(alice.Token);
        var aliceReq = await aliceClient.RequestBookAsync(book.Id);

        var bobClient = _factory.CreateClient();
        var bob = await bobClient.RegisterAsync("bob");
        bobClient.Authenticate(bob.Token);
        var bobReq = await bobClient.RequestBookAsync(book.Id);

        var accept = await ownerClient.PutAsJsonAsync(
            $"/api/borrowrequests/{aliceReq.Id}",
            new {status = "accepted"}
        );
        accept.EnsureSuccessStatusCode();

                var allRequests = await ownerClient.GetFromJsonAsync<BorrowResult[]>(
            "/api/borrowrequests"
        );
        var aliceAfter = allRequests!.Single(r => r.Id == aliceReq.Id);
        var bobAfter = allRequests!.Single(r => r.Id == bobReq.Id);

        Assert.Equal("accepted", aliceAfter.Status);
        Assert.Equal("declined", bobAfter.Status);

        var ownerLibrary = await ownerClient.GetFromJsonAsync<BookResult[]>("/api/library");
        var entryAfter = ownerLibrary!.Single(e => e.Id == book.Id);
        Assert.Equal("lent-out", entryAfter.Offer);
        Assert.Equal("read", entryAfter.Shelf);
    }

        [Fact]
    public async Task RequestingTheSameBookTwice_ReturnsBadRequest()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var book = await ownerClient.AddBookAsync("Popular Book");

        var borrowerClient = _factory.CreateClient();
        var borrower = await borrowerClient.RegisterAsync("borrower");
        borrowerClient.Authenticate(borrower.Token);

        await borrowerClient.RequestBookAsync(book.Id);

        var second = await borrowerClient.PostAsJsonAsync(
            "/api/borrowrequests",
            new { libraryEntryId = book.Id, message = "again" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Equal("You already have a pending request for this book", body!.Message);
    }

        [Fact]
    public async Task UpdatingRequestWithInvalidStatus_ReturnsBadRequest()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var book = await ownerClient.AddBookAsync("Some Book");

        var borrowerClient = _factory.CreateClient();
        var borrower = await borrowerClient.RegisterAsync("borrower");
        borrowerClient.Authenticate(borrower.Token);
        var req = await borrowerClient.RequestBookAsync(book.Id);

        var response = await ownerClient.PutAsJsonAsync(
            $"/api/borrowrequests/{req.Id}",
            new { status = "banana" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Equal("Status must be 'accepted' or 'declined'", body!.Message);
    }

    [Fact]
    public async Task WithdrawingYourOwnRequest_RemovesIt()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var book = await ownerClient.AddBookAsync("Wanted Book");

        var borrowerClient = _factory.CreateClient();
        var borrower = await borrowerClient.RegisterAsync("borrower");
        borrowerClient.Authenticate(borrower.Token);
        var req = await borrowerClient.RequestBookAsync(book.Id);

        var deleteResponse = await borrowerClient.DeleteAsync(
            $"/api/borrowrequests/{req.Id}"
        );
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var remaining = await borrowerClient.GetFromJsonAsync<BorrowResult[]>(
            "/api/borrowrequests"
        );
        Assert.DoesNotContain(remaining!, r => r.Id == req.Id);
    }

    [Fact]
    public async Task WithdrawingSomeoneElsesRequest_ReturnsForbidden()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var book = await ownerClient.AddBookAsync("Wanted Book");

        var borrowerClient = _factory.CreateClient();
        var borrower = await borrowerClient.RegisterAsync("borrower");
        borrowerClient.Authenticate(borrower.Token);
        var req = await borrowerClient.RequestBookAsync(book.Id);

        var deleteResponse = await ownerClient.DeleteAsync(
            $"/api/borrowrequests/{req.Id}"
        );
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task WithdrawingAnAcceptedRequest_ReturnsBadRequest()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var book = await ownerClient.AddBookAsync("Wanted Book");

        var borrowerClient = _factory.CreateClient();
        var borrower = await borrowerClient.RegisterAsync("borrower");
        borrowerClient.Authenticate(borrower.Token);
        var req = await borrowerClient.RequestBookAsync(book.Id);

        var accept = await ownerClient.PutAsJsonAsync(
            $"/api/borrowrequests/{req.Id}",
            new { status = "accepted" }
        );
        accept.EnsureSuccessStatusCode();

        var deleteResponse = await borrowerClient.DeleteAsync(
            $"/api/borrowrequests/{req.Id}"
        );
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
    }
}