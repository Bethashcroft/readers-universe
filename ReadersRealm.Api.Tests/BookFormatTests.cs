using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class BookFormatTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public BookFormatTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static Task<HttpResponseMessage> UpdateAsync(
        HttpClient client,
        int entryId,
        string shelf,
        string offer,
        string format
    ) =>
        client.PutAsJsonAsync(
            $"/api/library/{entryId}",
            new
            {
                shelf,
                offer,
                format,
            }
        );

    [Fact]
    public async Task TheFormatYouPickIsSavedOnYourCopy()
    {
        var sophie = await _factory.SignInAsync("sophie");

        var added = await sophie.AddBookAsync("Babel", format: "ebook");

        Assert.Equal("ebook", added.Format);
        Assert.Equal("ebook", (await sophie.GetLibraryAsync()).Single().Format);
    }

    [Fact]
    public async Task AnEbookCannotBeOfferedWhenYouAddIt()
    {
        var sophie = await _factory.SignInAsync("sophie");

        var response = await sophie.PostAsJsonAsync(
            "/api/library",
            new
            {
                title = "Babel",
                author = "R.F. Kuang",
                coverUrl = "x",
                isbn = "",
                shelf = "read",
                offer = "available-to-borrow",
                format = "ebook",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "Ebooks and audiobooks can't be lent out or sold.",
            await response.ErrorMessageAsync()
        );
    }

    [Fact]
    public async Task AnAudiobookCannotBeOfferedEither()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel", format: "audiobook");

        var response = await sophie.PostAsync($"/api/library/{book.Id}/offer", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "Ebooks and audiobooks can't be lent out or sold.",
            await response.ErrorMessageAsync()
        );
    }

    [Fact]
    public async Task APhysicalBookCanStillBeOffered()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel", format: "physical");

        await sophie.OfferBookAsync(book.Id);

        Assert.Equal("available-to-borrow", (await sophie.GetLibraryAsync()).Single().Offer);
    }

    [Fact]
    public async Task ABookWithNoFormatIsNotBlocked()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel");

        await sophie.OfferBookAsync(book.Id);

        Assert.Equal("available-to-borrow", (await sophie.GetLibraryAsync()).Single().Offer);
    }

    [Fact]
    public async Task YouCannotTurnAnOfferedBookIntoAnEbook()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel", format: "physical");
        await sophie.OfferBookAsync(book.Id);

        var response = await UpdateAsync(
            sophie,
            book.Id,
            "read",
            "available-to-borrow",
            "ebook"
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("physical", (await sophie.GetLibraryAsync()).Single().Format);
    }

    [Fact]
    public async Task AnEbookCannotBePutUpForSaleEither()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel", format: "ebook");

        var response = await UpdateAsync(sophie, book.Id, "read", "for-sale", "ebook");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("none", (await sophie.GetLibraryAsync()).Single().Offer);
    }

    [Fact]
    public async Task AnEbookYouAreNotLendingCanChangeFormatFreely()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel", format: "physical");

        var response = await UpdateAsync(sophie, book.Id, "read", "none", "audiobook");
        response.EnsureSuccessStatusCode();

        Assert.Equal("audiobook", (await sophie.GetLibraryAsync()).Single().Format);
    }

    [Fact]
    public async Task ASaveThatLeavesFormatOutKeepsWhatYouHad()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel", format: "physical");

        var response = await sophie.PutAsJsonAsync(
            $"/api/library/{book.Id}",
            new { shelf = "currently-reading", offer = "none" }
        );
        response.EnsureSuccessStatusCode();

        var after = (await sophie.GetLibraryAsync()).Single();
        Assert.Equal("physical", after.Format);
        Assert.Equal("currently-reading", after.Shelf);
    }

    [Fact]
    public async Task MadeUpFormatsAreRefused()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel");

        var response = await UpdateAsync(sophie, book.Id, "read", "none", "papyrus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TheOfferBookPickerSkipsEbooksAndAudiobooks()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await sophie.AddBookAsync("Babel", format: "physical");
        await sophie.AddBookAsync("Yellowface", format: "ebook");
        await sophie.AddBookAsync("The Poppy War", format: "audiobook");
        await sophie.AddBookAsync("Piranesi");

        var offerable = await sophie.GetLibraryAsync("?offerable=true");

        Assert.Equal(["Babel", "Piranesi"], offerable.Select(b => b.Title).Order());
    }
}
