using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record ReadingResult(int Id, DateTime FinishedDate);

public class ReadingHistoryTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public ReadingHistoryTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static Task<HttpResponseMessage> MoveAsync(
        HttpClient client,
        int entryId,
        string shelf
    ) => client.PutAsJsonAsync($"/api/library/{entryId}", new { shelf, offer = "none" });

    private static async Task<ReadingResult[]> ReadingsAsync(HttpClient client, int entryId) =>
        (await client.GetFromJsonAsync<ReadingResult[]>($"/api/library/{entryId}/readings"))!;

    private static Task<HttpResponseMessage> EditReadingAsync(
        HttpClient client,
        int readingId,
        DateTime finishedDate
    ) => client.PutAsJsonAsync($"/api/library/readings/{readingId}", new { finishedDate });

    private static Task<HttpResponseMessage> DeleteReadingAsync(
        HttpClient client,
        int readingId
    ) => client.DeleteAsync($"/api/library/readings/{readingId}");

    private static async Task<BookResult> FinishAsync(HttpClient client, string title)
    {
        var book = await client.AddBookAsync(title, shelf: "currently-reading");
        (await MoveAsync(client, book.Id, "read")).EnsureSuccessStatusCode();
        return book;
    }

    private static async Task<BookResult> ReadTwiceAsync(HttpClient client)
    {
        var book = await FinishAsync(client, "Babel");
        var first = (await ReadingsAsync(client, book.Id)).Single();
        await EditReadingAsync(client, first.Id, new DateTime(2024, 3, 11));

        await MoveAsync(client, book.Id, "currently-reading");
        await MoveAsync(client, book.Id, "read");

        return book;
    }

    [Fact]
    public async Task FinishingABookRecordsTheDay()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel", shelf: "currently-reading");

        Assert.Null(book.FinishedDate);
        Assert.Equal(0, book.TimesRead);

        (await MoveAsync(sophie, book.Id, "read")).EnsureSuccessStatusCode();

        var after = (await sophie.GetLibraryAsync()).Single();
        Assert.Equal(DateTime.UtcNow.Date, after.FinishedDate!.Value.Date);
        Assert.Equal(1, after.TimesRead);
    }

    [Fact]
    public async Task AddingABookStraightToReadLeavesTheDateBlank()
    {
        var sophie = await _factory.SignInAsync("sophie");

        var book = await sophie.AddBookAsync("Babel", shelf: "read");

        Assert.Null(book.FinishedDate);
        Assert.Equal(0, book.TimesRead);
    }

    [Fact]
    public async Task ShufflingShelvesInOneDayOnlyCountsOnce()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");

        await MoveAsync(sophie, book.Id, "currently-reading");
        await MoveAsync(sophie, book.Id, "read");
        await MoveAsync(sophie, book.Id, "tbr");
        await MoveAsync(sophie, book.Id, "read");

        Assert.Equal(1, (await sophie.GetLibraryAsync()).Single().TimesRead);
    }

    [Fact]
    public async Task MovingABookOffReadKeepsTheHistory()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");

        await MoveAsync(sophie, book.Id, "currently-reading");

        var after = (await sophie.GetLibraryAsync()).Single();
        Assert.Equal(1, after.TimesRead);
        Assert.NotNull(after.FinishedDate);
    }

    [Fact]
    public async Task ShelvingAsSomethingElseRecordsNothing()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel", shelf: "tbr");

        await MoveAsync(sophie, book.Id, "dnf");

        var after = (await sophie.GetLibraryAsync()).Single();
        Assert.Equal(0, after.TimesRead);
        Assert.Null(after.FinishedDate);
    }

    [Fact]
    public async Task AnAccidentalReadCanBeDeletedLongAfterwards()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");
        await MoveAsync(sophie, book.Id, "currently-reading");

        var mistake = (await ReadingsAsync(sophie, book.Id)).Single();
        (await DeleteReadingAsync(sophie, mistake.Id)).EnsureSuccessStatusCode();

        var after = (await sophie.GetLibraryAsync()).Single();
        Assert.Equal(0, after.TimesRead);
        Assert.Null(after.FinishedDate);
    }

    [Fact]
    public async Task ARereadKeepsBothDatesNewestFirst()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await ReadTwiceAsync(sophie);

        var readings = await ReadingsAsync(sophie, book.Id);

        Assert.Equal(2, readings.Length);
        Assert.Equal(DateTime.UtcNow.Date, readings[0].FinishedDate.Date);
        Assert.Equal(new DateTime(2024, 3, 11), readings[1].FinishedDate);
        Assert.Equal(2, (await sophie.GetLibraryAsync()).Single().TimesRead);
    }

    [Fact]
    public async Task ADateCanBeCorrected()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");
        var today = (await ReadingsAsync(sophie, book.Id)).Single();

        (await EditReadingAsync(sophie, today.Id, new DateTime(2024, 4, 11)))
            .EnsureSuccessStatusCode();

        Assert.Equal(
            new DateTime(2024, 4, 11),
            (await sophie.GetLibraryAsync()).Single().FinishedDate
        );
    }

    [Fact]
    public async Task YouCannotFinishABookInTheFuture()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");
        var today = (await ReadingsAsync(sophie, book.Id)).Single();

        var response = await EditReadingAsync(sophie, today.Id, DateTime.UtcNow.AddDays(2));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "You can't finish a book in the future.",
            await response.ErrorMessageAsync()
        );
    }

    [Fact]
    public async Task ADateTooFarBackIsRefused()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");
        var today = (await ReadingsAsync(sophie, book.Id)).Single();

        var response = await EditReadingAsync(sophie, today.Id, new DateTime(24, 3, 11));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("That date is too far back.", await response.ErrorMessageAsync());
    }

    [Fact]
    public async Task TodayCountsWhereverYouAre()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");
        var today = (await ReadingsAsync(sophie, book.Id)).Single();
        var aheadOfLondon = DateTime.UtcNow.AddHours(13).Date;

        (await EditReadingAsync(sophie, today.Id, aheadOfLondon)).EnsureSuccessStatusCode();

        Assert.Equal(aheadOfLondon, (await sophie.GetLibraryAsync()).Single().FinishedDate);
    }

    [Fact]
    public async Task FinishingUsesTheDateOnYourDevice()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel", shelf: "currently-reading");
        var yourToday = DateTime.UtcNow.AddHours(13).Date;

        await sophie.PutAsJsonAsync(
            $"/api/library/{book.Id}",
            new
            {
                shelf = "read",
                offer = "none",
                today = yourToday,
            }
        );

        Assert.Equal(yourToday, (await sophie.GetLibraryAsync()).Single().FinishedDate);
    }

    [Fact]
    public async Task ADeviceDateThatCannotBeTodayIsIgnored()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await sophie.AddBookAsync("Babel", shelf: "currently-reading");

        await sophie.PutAsJsonAsync(
            $"/api/library/{book.Id}",
            new
            {
                shelf = "read",
                offer = "none",
                today = new DateTime(2020, 1, 1),
            }
        );

        Assert.Equal(
            DateTime.UtcNow.Date,
            (await sophie.GetLibraryAsync()).Single().FinishedDate!.Value.Date
        );
    }

    [Fact]
    public async Task DeletingAFinishTakesItsPostOutOfTheFeed()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var beth = await _factory.SignInAsync("beth");
        await beth.PostAsync("/api/users/sophie/follow", null);
        var book = await FinishAsync(sophie, "Babel");

        var feed = await beth.GetFromJsonAsync<ActivityResult[]>("/api/feed");
        Assert.Contains(feed!, a => a.Type == "finished");

        var mistake = (await ReadingsAsync(sophie, book.Id)).Single();
        await DeleteReadingAsync(sophie, mistake.Id);

        var after = await beth.GetFromJsonAsync<ActivityResult[]>("/api/feed");
        Assert.DoesNotContain(after!, a => a.Type == "finished");
        Assert.Contains(after!, a => a.Type == "started-reading");
    }

    private static async Task<ActivityResult[]> FinishedPostsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ActivityResult[]>("/api/users/sophie/activity"))!
            .Where(a => a.Type == "finished")
            .ToArray();

    [Fact]
    public async Task ChangingTheDateThenDeletingStillTakesThePostDown()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");
        var finish = (await ReadingsAsync(sophie, book.Id)).Single();
        await EditReadingAsync(sophie, finish.Id, new DateTime(2024, 3, 11));

        await DeleteReadingAsync(sophie, finish.Id);

        Assert.Empty(await FinishedPostsAsync(sophie));
    }

    [Fact]
    public async Task DeletingOneFinishOfAReReadKeepsTheOtherPost()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await ReadTwiceAsync(sophie);
        Assert.Equal(2, (await FinishedPostsAsync(sophie)).Length);

        var older = (await ReadingsAsync(sophie, book.Id))[1];
        await DeleteReadingAsync(sophie, older.Id);

        Assert.Single(await FinishedPostsAsync(sophie));
    }

    [Fact]
    public async Task RemovingABookFromYourShelvesKeepsItsFinishedPost()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");

        (await sophie.DeleteAsync($"/api/library/{book.Id}")).EnsureSuccessStatusCode();

        Assert.Single(await FinishedPostsAsync(sophie));
    }

    [Fact]
    public async Task OtherReadersDoNotGetYourFinishDates()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");
        await sophie.OfferBookAsync(book.Id);

        var tom = await _factory.SignInAsync("tom");
        var onBrowse = (await tom.GetBrowseAsync()).Single();
        var onHerProfile = (
            await tom.GetFromJsonAsync<BookResult[]>("/api/users/sophie/books")
        )!.Single();
        var herOwnView = (
            await sophie.GetFromJsonAsync<BookResult[]>("/api/users/sophie/books")
        )!.Single();

        Assert.Null(onBrowse.FinishedDate);
        Assert.Equal(0, onBrowse.TimesRead);
        Assert.Null(onHerProfile.FinishedDate);
        Assert.Equal(1, herOwnView.TimesRead);
    }

    [Fact]
    public async Task TwoFinishesCannotShareADay()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await ReadTwiceAsync(sophie);
        var older = (await ReadingsAsync(sophie, book.Id))[1];

        var response = await EditReadingAsync(sophie, older.Id, DateTime.UtcNow);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("That day is already on your list.", await response.ErrorMessageAsync());
    }

    [Fact]
    public async Task YouCannotTouchSomeoneElsesHistory()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");
        var hers = (await ReadingsAsync(sophie, book.Id)).Single();

        var tom = await _factory.SignInAsync("tom");

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await EditReadingAsync(tom, hers.Id, new DateTime(2024, 1, 1))).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await DeleteReadingAsync(tom, hers.Id)).StatusCode
        );
        Assert.Equal(
            DateTime.UtcNow.Date,
            (await ReadingsAsync(sophie, book.Id)).Single().FinishedDate.Date
        );
    }

    [Fact]
    public async Task SortingByDateFinishedPutsTheNewestFirstAndUndatedLast()
    {
        var sophie = await _factory.SignInAsync("sophie");

        async Task FinishedOnAsync(string title, DateTime day)
        {
            var book = await FinishAsync(sophie, title);
            var reading = (await ReadingsAsync(sophie, book.Id)).Single();
            await EditReadingAsync(sophie, reading.Id, day);
        }

        await FinishedOnAsync("Babel", new DateTime(2024, 3, 11));
        await FinishAsync(sophie, "Piranesi");
        await sophie.AddBookAsync("Yellowface", shelf: "tbr");
        await FinishedOnAsync("The Poppy War", new DateTime(2025, 6, 1));

        var sorted = await sophie.GetLibraryAsync("?sort=finished");

        Assert.Equal(
            ["Piranesi", "The Poppy War", "Babel", "Yellowface"],
            sorted.Select(b => b.Title)
        );

        var flipped = await sophie.GetLibraryAsync("?sort=finished&reverse=true");

        Assert.Equal(
            ["Babel", "The Poppy War", "Piranesi", "Yellowface"],
            flipped.Select(b => b.Title)
        );
    }

    [Fact]
    public async Task FlippingTitleAndRatingKeepsUnratedBooksLast()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await sophie.AddBookAsync("Babel", rating: 5);
        await sophie.AddBookAsync("Piranesi", rating: 2);
        await sophie.AddBookAsync("Yellowface");

        var zToA = await sophie.GetLibraryAsync("?sort=title&reverse=true");
        var lowestFirst = await sophie.GetLibraryAsync("?sort=rating&reverse=true");
        var highestFirst = await sophie.GetLibraryAsync("?sort=rating");

        Assert.Equal(["Yellowface", "Piranesi", "Babel"], zToA.Select(b => b.Title));
        Assert.Equal(["Piranesi", "Babel", "Yellowface"], lowestFirst.Select(b => b.Title));
        Assert.Equal(["Babel", "Piranesi", "Yellowface"], highestFirst.Select(b => b.Title));
    }

    [Fact]
    public async Task FlippingRecentlyAddedShowsTheOldestFirst()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await sophie.AddBookAsync("Babel");
        await sophie.AddBookAsync("Piranesi");

        var oldest = await sophie.GetLibraryAsync("?reverse=true");

        Assert.Equal(["Babel", "Piranesi"], oldest.Select(b => b.Title));
    }

    [Fact]
    public async Task YourHistoryIsYoursNotTheBooks()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var hers = await FinishAsync(sophie, "Babel");

        var tom = await _factory.SignInAsync("tom");
        var his = await tom.PostAsJsonAsync(
            "/api/library",
            new
            {
                bookId = hers.BookId,
                shelf = "tbr",
                offer = "none",
            }
        );
        his.EnsureSuccessStatusCode();

        Assert.Equal(1, (await sophie.GetLibraryAsync()).Single().TimesRead);
        Assert.Equal(0, (await tom.GetLibraryAsync()).Single().TimesRead);
    }
}
