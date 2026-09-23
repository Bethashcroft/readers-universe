using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class AccountDeletionTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public AccountDeletionTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static Task<HttpResponseMessage> DeleteAccountAsync(
        HttpClient client,
        string confirmUserName
    ) =>
        client.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, "/api/auth/account")
            {
                Content = JsonContent.Create(new { confirmUserName }),
            }
        );

    private static async Task<BookResult> FinishAsync(HttpClient client, string title)
    {
        var book = await client.AddBookAsync(title, shelf: "currently-reading");
        await client.PutAsJsonAsync(
            $"/api/library/{book.Id}",
            new { shelf = "read", offer = "none" }
        );
        return book;
    }

    [Fact]
    public async Task DeletingYourAccountRemovesEverythingOfYoursAndNothingElse()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var tom = await _factory.SignInAsync("tom");

        await tom.PostAsync("/api/users/sophie/follow", null);
        await sophie.PostAsync("/api/users/tom/follow", null);
        await sophie.TrustAsync("tom");
        await tom.TrustAsync("sophie");

        var hers = await FinishAsync(sophie, "Babel");
        await sophie.AddReviewAsync(hers.BookId, text: "Loved it");
        await sophie.PutAsJsonAsync($"/api/goals/{DateTime.UtcNow.Year}", new { target = 20 });

        var his = await FinishAsync(tom, "Piranesi");
        await tom.OfferBookAsync(his.Id);
        var request = await sophie.RequestBookAsync(his.Id);
        await sophie.PostAsJsonAsync(
            "/api/messages",
            new { borrowRequestId = request.Id, text = "Can I borrow this?" }
        );

        var hisPost = (
            await sophie.GetFromJsonAsync<ActivityResult[]>("/api/feed")
        )!.First(a => a.UserName == "tom");
        await sophie.PostAsync($"/api/feed/{hisPost.Id}/like", null);
        await sophie.PostAsJsonAsync(
            $"/api/feed/{hisPost.Id}/comments",
            new { text = "Great choice" }
        );

        var response = await DeleteAccountAsync(sophie, "sophie");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var gone = await tom.GetAsync("/api/users/sophie");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);

        var hisPostNow = (
            await tom.GetFromJsonAsync<ActivityResult[]>("/api/users/tom/activity")
        )!.Single(a => a.Id == hisPost.Id);
        Assert.Equal(0, hisPostNow.LikeCount);
        Assert.Equal(0, hisPostNow.CommentCount);

        Assert.Equal("available-to-borrow", (await tom.GetLibraryAsync()).Single().Offer);
        Assert.Empty((await tom.GetBorrowingAsync()).Incoming);

        var reviews = await tom.GetFromJsonAsync<ReviewResult[]>(
            $"/api/reviews/book/{hers.BookId}"
        );
        Assert.Empty(reviews!);
    }

    [Fact]
    public async Task YouHaveToTypeYourUsernameToConfirm()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await sophie.AddBookAsync("Babel");

        var response = await DeleteAccountAsync(sophie, "not-sophie");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(await sophie.GetLibraryAsync());
    }

    [Fact]
    public async Task ABookYouWereBorrowingIsFreedForItsOwner()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var tom = await _factory.SignInAsync("tom");
        await tom.TrustAsync("sophie");

        var his = await tom.AddBookAsync("Piranesi");
        await tom.OfferBookAsync(his.Id);
        var request = await sophie.RequestBookAsync(his.Id);
        await tom.AcceptRequestAsync(request.Id);
        Assert.Equal("lent-out", (await tom.GetLibraryAsync()).Single().Offer);

        await DeleteAccountAsync(sophie, "sophie");

        Assert.Equal("none", (await tom.GetLibraryAsync()).Single().Offer);
    }

    [Fact]
    public async Task YourUsernameAndEmailAreFreeAgainAfterwards()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await DeleteAccountAsync(sophie, "sophie");

        var again = _factory.CreateClient();
        var fresh = await again.RegisterAsync("sophie");

        Assert.Equal("sophie", fresh.UserName);
    }

    [Fact]
    public async Task AGoogleAccountCanBeDeletedToo()
    {
        var client = _factory.CreateClient();
        var token = FakeGoogleTokenValidator.TokenFor("g-1", "reader@gmail.com", "Reader");
        var created = await client.PostAsJsonAsync(
            "/api/auth/google/register",
            new
            {
                idToken = token,
                userName = "bookdragon",
                displayName = "Reader",
            }
        );
        var auth = (await created.Content.ReadFromJsonAsync<AuthResult>())!;
        client.Authenticate(auth.Token);

        var response = await DeleteAccountAsync(client, "bookdragon");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
