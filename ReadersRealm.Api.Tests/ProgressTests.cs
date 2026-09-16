using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class ProgressTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public ProgressTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static Task<HttpResponseMessage> ProgressAsync(
        HttpClient client,
        int entryId,
        int? page,
        int? pageCount
    ) => client.PutAsJsonAsync($"/api/library/{entryId}/progress", new { page, pageCount });

    private static Task<HttpResponseMessage> LookUpAsync(HttpClient client, int entryId) =>
        client.PostAsync($"/api/library/{entryId}/page-count", null);

    private static async Task<ActivityResult[]> FeedAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ActivityResult[]>("/api/feed"))!;

    [Fact]
    public async Task YourPageAndPageCountAreSavedOnYourCopy()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Piranesi", shelf: "currently-reading");

        var response = await ProgressAsync(sophie, book.Id, 120, 340);
        response.EnsureSuccessStatusCode();

        var saved = (await response.Content.ReadFromJsonAsync<BookResult>())!;
        Assert.Equal(120, saved.Page);
        Assert.Equal(340, saved.PageCount);

        var fromShelves = (await sophie.GetLibraryAsync()).Single();
        Assert.Equal(120, fromShelves.Page);
        Assert.Equal(340, fromShelves.PageCount);
    }

    [Fact]
    public async Task ThePageCountIsYoursNotTheBooks()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var hers = await sophie.AddBookAsync("Piranesi", shelf: "currently-reading");
        await ProgressAsync(sophie, hers.Id, 10, 340);

        var tom = await _factory.SignInAsync("tom");
        var his = await tom.PostAsJsonAsync(
            "/api/library",
            new { bookId = hers.BookId, shelf = "currently-reading", offer = "none" }
        );
        var hisEntry = (await his.Content.ReadFromJsonAsync<BookResult>())!;

        Assert.Null(hisEntry.PageCount);

        await ProgressAsync(tom, hisEntry.Id, 10, 480);

        Assert.Equal(340, (await sophie.GetLibraryAsync()).Single().PageCount);
        Assert.Equal(480, (await tom.GetLibraryAsync()).Single().PageCount);
    }

    [Fact]
    public async Task UpdatingYourPageShowsInTheFeed()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var beth = await _factory.SignInAsync("beth");
        await beth.PostAsync("/api/users/sophie/follow", null);
        var book = await sophie.AddBookAsync("Piranesi", shelf: "currently-reading");

        await ProgressAsync(sophie, book.Id, 120, 340);

        var progress = (await FeedAsync(beth)).First();
        Assert.Equal("progress", progress.Type);
        Assert.Equal(120, progress.Page);
        Assert.Equal(340, progress.PageCount);
        Assert.Equal("Piranesi", progress.Book!.Title);
    }

    [Fact]
    public async Task EveryPageUpdateIsAnEventButFixingTheCountIsNot()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Piranesi", shelf: "currently-reading");

        await ProgressAsync(sophie, book.Id, 50, 340);
        await ProgressAsync(sophie, book.Id, 60, 340);
        await ProgressAsync(sophie, book.Id, 60, 350);

        var activity = await sophie.GetFromJsonAsync<ActivityResult[]>(
            "/api/users/sophie/activity"
        );

        Assert.Equal(2, activity!.Count(a => a.Type == "progress"));
    }

    [Fact]
    public async Task YouCannotBePastTheLastPage()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Piranesi", shelf: "currently-reading");

        var tooFar = await ProgressAsync(sophie, book.Id, 400, 340);
        var negative = await ProgressAsync(sophie, book.Id, -1, 340);
        var zeroPages = await ProgressAsync(sophie, book.Id, 1, 0);

        Assert.Equal(HttpStatusCode.BadRequest, tooFar.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, zeroPages.StatusCode);
        Assert.Equal(
            "You can't be past the last page.",
            await tooFar.ErrorMessageAsync()
        );
    }

    [Fact]
    public async Task APageWithoutATotalIsFine()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Piranesi", shelf: "currently-reading");

        (await ProgressAsync(sophie, book.Id, 120, null)).EnsureSuccessStatusCode();

        var saved = (await sophie.GetLibraryAsync()).Single();
        Assert.Equal(120, saved.Page);
        Assert.Null(saved.PageCount);
    }

    [Fact]
    public async Task YouCannotUpdateSomeoneElsesProgress()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Piranesi", shelf: "currently-reading");

        var tom = await _factory.SignInAsync("tom");
        var response = await ProgressAsync(tom, book.Id, 5, 340);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AMissingPageCountIsFilledInFromTheIsbn()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync(
            "The Hobbit",
            shelf: "currently-reading",
            isbn: FakeBookLookup.KnownIsbn
        );
        Assert.Null(book.PageCount);

        var response = await LookUpAsync(sophie, book.Id);
        response.EnsureSuccessStatusCode();

        var filled = (await response.Content.ReadFromJsonAsync<BookResult>())!;
        Assert.Equal(310, filled.PageCount);
        Assert.Equal(310, (await sophie.GetLibraryAsync()).Single().PageCount);
    }

    [Fact]
    public async Task WithoutAnIsbnTheTitleIsSearchedInstead()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync(FakeBookLookup.KnownTitle, shelf: "currently-reading");

        var response = await LookUpAsync(sophie, book.Id);
        response.EnsureSuccessStatusCode();

        var filled = (await response.Content.ReadFromJsonAsync<BookResult>())!;
        Assert.Equal(569, filled.PageCount);
    }

    [Fact]
    public async Task ACountYouTypedIsNeverOverwritten()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync(
            "The Hobbit",
            shelf: "currently-reading",
            isbn: FakeBookLookup.KnownIsbn
        );
        await ProgressAsync(sophie, book.Id, 10, 300);

        var response = await LookUpAsync(sophie, book.Id);
        var after = (await response.Content.ReadFromJsonAsync<BookResult>())!;

        Assert.Equal(300, after.PageCount);
    }

    [Fact]
    public async Task AnUnknownIsbnLeavesTheCountBlank()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync(
            "Mystery",
            shelf: "currently-reading",
            isbn: FakeBookLookup.UnknownIsbn
        );

        var response = await LookUpAsync(sophie, book.Id);
        response.EnsureSuccessStatusCode();

        var after = (await response.Content.ReadFromJsonAsync<BookResult>())!;
        Assert.Null(after.PageCount);
    }
}
