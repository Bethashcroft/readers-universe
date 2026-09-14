using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class BorrowingTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public BorrowingTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient Owner, HttpClient Borrower, BookResult Book, BorrowResult Request)> LoanAsync()
    {
        var owner = await _factory.SignInAsync("owner");
        var book = await owner.AddBookAsync("Piranesi", offer: "available-to-borrow");

        var borrower = await _factory.SignInAsync("borrower");
        await owner.TrustAsync("borrower");
        var request = await borrower.RequestBookAsync(book.Id);

        return (owner, borrower, book, request);
    }

    [Fact]
    public async Task PendingRequestsShowAsIncomingForTheOwnerAndOutgoingForTheBorrower()
    {
        var (owner, borrower, _, request) = await LoanAsync();

        var ownerView = await owner.GetBorrowingAsync();
        var borrowerView = await borrower.GetBorrowingAsync();

        Assert.Equal(request.Id, ownerView.Incoming.Single().Id);
        Assert.Empty(ownerView.Outgoing);
        Assert.Equal(request.Id, borrowerView.Outgoing.Single().Id);
        Assert.Empty(borrowerView.Incoming);
        Assert.Equal(3, ownerView.Limit);
    }

    [Fact]
    public async Task AnAcceptedLoanShowsAsLentOutAndBorrowed()
    {
        var (owner, borrower, book, request) = await LoanAsync();
        await owner.AddBookAsync("Circe", offer: "available-to-borrow");
        await owner.AcceptRequestAsync(request.Id);

        var ownerView = await owner.GetBorrowingAsync();
        var lent = ownerView.Offering.Single(o => o.LibraryEntryId == book.Id);

        Assert.Equal(2, ownerView.Offering.Length);
        Assert.Equal("lent-out", lent.Offer);
        Assert.Equal(request.Id, lent.Borrower!.RequestId);
        Assert.Equal("borrower", lent.Borrower.UserName);
        Assert.Null(ownerView.Offering.Single(o => o.LibraryEntryId != book.Id).Borrower);
        Assert.Empty(ownerView.Incoming);

        var borrowerView = await borrower.GetBorrowingAsync();
        Assert.Equal(book.BookId, borrowerView.Borrowed.Single().BookId);
        Assert.Empty(borrowerView.Outgoing);
    }

    [Fact]
    public async Task MarkingReturnedPutsTheBookBackOnOffer()
    {
        var (owner, borrower, book, request) = await LoanAsync();
        await owner.AcceptRequestAsync(request.Id);

        var returned = await owner.PostAsync($"/api/borrowrequests/{request.Id}/return", null);
        returned.EnsureSuccessStatusCode();
        Assert.Equal("returned", (await returned.Content.ReadFromJsonAsync<BorrowResult>())!.Status);

        var offered = (await owner.GetBorrowingAsync()).Offering.Single(o => o.LibraryEntryId == book.Id);
        Assert.Equal("available-to-borrow", offered.Offer);
        Assert.Null(offered.Borrower);

        var borrowerView = await borrower.GetBorrowingAsync();
        Assert.Empty(borrowerView.Borrowed);
        Assert.Equal("returned", borrowerView.History.Single().Status);
    }

    [Fact]
    public async Task OnlyTheOwnerCanMarkABookReturned()
    {
        var (owner, borrower, _, request) = await LoanAsync();
        await owner.AcceptRequestAsync(request.Id);

        var response = await borrower.PostAsync($"/api/borrowrequests/{request.Id}/return", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OnlyALentBookCanBeMarkedReturned()
    {
        var (owner, _, _, request) = await LoanAsync();

        var response = await owner.PostAsync($"/api/borrowrequests/{request.Id}/return", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnAnsweredRequestCannotBeAnsweredAgain()
    {
        var (owner, borrower, book, request) = await LoanAsync();
        await owner.AcceptRequestAsync(request.Id);

        var decline = await owner.PutAsJsonAsync(
            $"/api/borrowrequests/{request.Id}",
            new { status = "declined" }
        );
        Assert.Equal(HttpStatusCode.BadRequest, decline.StatusCode);

        await owner.PostAsync($"/api/borrowrequests/{request.Id}/return", null);

        var reaccept = await owner.PutAsJsonAsync(
            $"/api/borrowrequests/{request.Id}",
            new { status = "accepted" }
        );
        Assert.Equal(HttpStatusCode.BadRequest, reaccept.StatusCode);
        Assert.Empty((await borrower.GetBorrowingAsync()).Borrowed);
        Assert.Equal(
            "available-to-borrow",
            (await owner.GetLibraryAsync()).Single(e => e.Id == book.Id).Offer
        );
    }

    [Fact]
    public async Task ADeclinedRequestStaysInHistoryAndCannotBeDeleted()
    {
        var (owner, borrower, _, request) = await LoanAsync();
        await owner.PutAsJsonAsync($"/api/borrowrequests/{request.Id}", new { status = "declined" });

        var borrowerView = await borrower.GetBorrowingAsync();
        Assert.Empty(borrowerView.Outgoing);
        Assert.Equal("declined", borrowerView.History.Single().Status);
        Assert.Equal("declined", (await owner.GetBorrowingAsync()).History.Single().Status);

        var delete = await borrower.DeleteAsync($"/api/borrowrequests/{request.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
        Assert.Single((await borrower.GetBorrowingAsync()).History);
    }

    [Fact]
    public async Task TheNavBadgeCountsIncomingPendingRequests()
    {
        var (owner, borrower, _, _) = await LoanAsync();

        var ownerCount = await owner.GetFromJsonAsync<UnreadCountResult>(
            "/api/borrowrequests/pending-count"
        );
        var borrowerCount = await borrower.GetFromJsonAsync<UnreadCountResult>(
            "/api/borrowrequests/pending-count"
        );

        Assert.Equal(1, ownerCount!.Count);
        Assert.Equal(0, borrowerCount!.Count);
    }
}
