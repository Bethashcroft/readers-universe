using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class DashboardTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public DashboardTests()
    {
        _factory = new TestWebAppFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private record DashboardResult(int MyBooks, int Nearby, int PendingRequests);

    [Fact]
    public async Task Summary_CountsOwnBooksNearbyBooksAndIncomingPendingRequests()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var book = await ownerClient.AddBookAsync(
            "Owner Borrow Book",
            offer: "available-to-borrow"
        );
        await ownerClient.AddBookAsync("Owner Sale Book", offer: "for-sale");

        var borrowerClient = _factory.CreateClient();
        var borrower = await borrowerClient.RegisterAsync("borrower");
        borrowerClient.Authenticate(borrower.Token);
        await borrowerClient.AddBookAsync("Borrower Book", offer: "available-to-borrow");
        await borrowerClient.RequestBookAsync(book.Id);

        var summary = await ownerClient.GetFromJsonAsync<DashboardResult>(
            "/api/dashboard/summary"
        );

        Assert.Equal(2, summary!.MyBooks);
        Assert.Equal(1, summary.Nearby);
        Assert.Equal(1, summary.PendingRequests);
    }
}
