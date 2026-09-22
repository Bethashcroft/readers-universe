using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class BorrowNotificationTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public BorrowNotificationTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static async Task<NotificationResult[]> NotificationsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<NotificationResult[]>("/api/notifications"))!;

    private static async Task<NotificationResult> BorrowPingAsync(
        HttpClient client,
        string type
    ) => Assert.Single(await NotificationsAsync(client), n => n.Type == type);

    private async Task<(HttpClient Sophie, HttpClient Tom, int EntryId)> SophieOffersABookAsync()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var tom = await _factory.SignInAsync("tom");

        var book = await sophie.AddBookAsync("Babel", shelf: "read");
        await sophie.OfferBookAsync(book.Id);
        await sophie.TrustAsync("tom");

        return (sophie, tom, book.Id);
    }

    [Fact]
    public async Task AskingToBorrowTellsTheOwner()
    {
        var (sophie, tom, entryId) = await SophieOffersABookAsync();

        await tom.RequestBookAsync(entryId);

        var ping = await BorrowPingAsync(sophie, "borrow-requested");
        Assert.Equal("tom", ping.ActorUserName);
        Assert.Equal("Babel", ping.BookTitle);
    }

    [Fact]
    public async Task SayingYesTellsTheBorrower()
    {
        var (sophie, tom, entryId) = await SophieOffersABookAsync();
        var request = await tom.RequestBookAsync(entryId);

        await sophie.AcceptRequestAsync(request.Id);

        var ping = await BorrowPingAsync(tom, "borrow-accepted");
        Assert.Equal("sophie", ping.ActorUserName);
        Assert.Equal("Babel", ping.BookTitle);
    }

    [Fact]
    public async Task SayingNoTellsTheBorrower()
    {
        var (sophie, tom, entryId) = await SophieOffersABookAsync();
        var request = await tom.RequestBookAsync(entryId);

        await sophie.PutAsJsonAsync(
            $"/api/borrowrequests/{request.Id}",
            new { status = "declined" }
        );

        var ping = await BorrowPingAsync(tom, "borrow-declined");
        Assert.Equal("Babel", ping.BookTitle);
    }

    [Fact]
    public async Task MarkingABookReturnedTellsTheBorrower()
    {
        var (sophie, tom, entryId) = await SophieOffersABookAsync();
        var request = await tom.RequestBookAsync(entryId);
        await sophie.AcceptRequestAsync(request.Id);

        var response = await sophie.PostAsync($"/api/borrowrequests/{request.Id}/return", null);
        response.EnsureSuccessStatusCode();

        var ping = await BorrowPingAsync(tom, "borrow-returned");
        Assert.Equal("Babel", ping.BookTitle);
    }

    [Fact]
    public async Task TakingABookBackTellsEveryoneWaiting()
    {
        var (sophie, tom, entryId) = await SophieOffersABookAsync();
        await tom.RequestBookAsync(entryId);

        var response = await sophie.DeleteAsync($"/api/library/{entryId}/offer");
        response.EnsureSuccessStatusCode();

        Assert.Equal("Babel", (await BorrowPingAsync(tom, "borrow-declined")).BookTitle);
    }

    [Fact]
    public async Task AcceptingOneRequestTellsTheOthersNo()
    {
        var (sophie, tom, entryId) = await SophieOffersABookAsync();
        var nadia = await _factory.SignInAsync("nadia");
        await sophie.TrustAsync("nadia");

        var tomsRequest = await tom.RequestBookAsync(entryId);
        await nadia.RequestBookAsync(entryId);

        await sophie.AcceptRequestAsync(tomsRequest.Id);

        Assert.Equal("Babel", (await BorrowPingAsync(tom, "borrow-accepted")).BookTitle);
        Assert.Equal("Babel", (await BorrowPingAsync(nadia, "borrow-declined")).BookTitle);
    }

    [Fact]
    public async Task UntrustingSomeoneTellsThemTheirRequestIsOff()
    {
        var (sophie, tom, entryId) = await SophieOffersABookAsync();
        await tom.RequestBookAsync(entryId);

        var response = await sophie.DeleteAsync("/api/users/tom/trust");
        response.EnsureSuccessStatusCode();

        Assert.Equal("Babel", (await BorrowPingAsync(tom, "borrow-declined")).BookTitle);
    }

    [Fact]
    public async Task WithdrawingYourOwnRequestTellsNobody()
    {
        var (sophie, tom, entryId) = await SophieOffersABookAsync();
        var request = await tom.RequestBookAsync(entryId);

        var response = await tom.DeleteAsync($"/api/borrowrequests/{request.Id}");
        response.EnsureSuccessStatusCode();

        var hers = (await NotificationsAsync(sophie)).Where(n => n.Type.StartsWith("borrow-"));
        var his = (await NotificationsAsync(tom)).Where(n => n.Type.StartsWith("borrow-"));

        Assert.Equal("borrow-requested", Assert.Single(hers).Type);
        Assert.Empty(his);
    }
}
