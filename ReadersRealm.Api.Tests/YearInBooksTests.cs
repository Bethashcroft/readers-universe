using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record YearBookResult(int BookId, string Title, int? PageCount, int? Rating);

public record AuthorCountResult(string Name, int Books);

public record YearInBooksResult(
    int Year,
    int? Target,
    int BooksRead,
    int PagesRead,
    int BooksWithoutPageCount,
    double? AverageRating,
    int Rereads,
    int[] Months,
    Dictionary<string, int> Formats,
    YearBookResult? Longest,
    YearBookResult? Shortest,
    AuthorCountResult? TopAuthor,
    YearBookResult[] FiveStars,
    YearBookResult[] Books
);

public class YearInBooksTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private static readonly int LastYear = DateTime.UtcNow.Year - 1;

    public YearInBooksTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static async Task<YearInBooksResult> YearAsync(HttpClient client, int year) =>
        (await client.GetFromJsonAsync<YearInBooksResult>($"/api/year-in-books/{year}"))!;

    private static async Task<BookResult> FinishOnAsync(
        HttpClient client,
        string title,
        DateTime day,
        string author = "Test Author",
        int? pages = null,
        string format = "",
        int? rating = null
    )
    {
        var book = await client.AddBookAsync(
            title,
            shelf: "currently-reading",
            author: author,
            rating: rating,
            format: format
        );

        if (pages != null)
        {
            await client.PutAsJsonAsync(
                $"/api/library/{book.Id}/progress",
                new { page = (int?)null, pageCount = pages }
            );
        }

        await FinishAgainOnAsync(client, book, day);
        return book;
    }

    private static async Task FinishAgainOnAsync(HttpClient client, BookResult book, DateTime day)
    {
        await client.PutAsJsonAsync(
            $"/api/library/{book.Id}",
            new { shelf = "currently-reading", offer = "none" }
        );
        await client.PutAsJsonAsync($"/api/library/{book.Id}", new { shelf = "read", offer = "none" });

        var newest = (
            await client.GetFromJsonAsync<ReadingResult[]>($"/api/library/{book.Id}/readings")
        )!.First();
        await client.PutAsJsonAsync(
            $"/api/library/readings/{newest.Id}",
            new { finishedDate = day }
        );
    }

    [Fact]
    public async Task TheYearAddsUpWhatYouFinished()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await FinishOnAsync(sophie, "Piranesi", new DateTime(LastYear, 1, 10), pages: 300, format: "physical", rating: 4);
        await FinishOnAsync(sophie, "Babel", new DateTime(LastYear, 3, 2), pages: 500, format: "ebook", rating: 5);
        await FinishOnAsync(sophie, "The Poppy War", new DateTime(LastYear, 3, 20), format: "audiobook");
        await FinishOnAsync(sophie, "Yellowface", new DateTime(LastYear + 1, 1, 1), pages: 330);
        await sophie.PutAsJsonAsync($"/api/goals/{LastYear}", new { target = 10 });

        var year = await YearAsync(sophie, LastYear);

        Assert.Equal(10, year.Target);
        Assert.Equal(3, year.BooksRead);
        Assert.Equal(800, year.PagesRead);
        Assert.Equal(1, year.BooksWithoutPageCount);
        Assert.Equal(4.5, year.AverageRating);
        Assert.Equal([1, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0], year.Months);
        Assert.Equal(1, year.Formats["physical"]);
        Assert.Equal(1, year.Formats["ebook"]);
        Assert.Equal(1, year.Formats["audiobook"]);
        Assert.Equal(0, year.Formats["unset"]);
        Assert.Equal("Babel", year.Longest!.Title);
        Assert.Equal("Piranesi", year.Shortest!.Title);
        Assert.Equal("Babel", Assert.Single(year.FiveStars).Title);
        Assert.Equal(["Piranesi", "Babel", "The Poppy War"], year.Books.Select(b => b.Title));
    }

    [Fact]
    public async Task TheMostReadAuthorNeedsAtLeastTwoBooks()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await FinishOnAsync(sophie, "Babel", new DateTime(LastYear, 2, 1), author: "R.F. Kuang");
        await FinishOnAsync(sophie, "Piranesi", new DateTime(LastYear, 2, 2), author: "Susanna Clarke");

        Assert.Null((await YearAsync(sophie, LastYear)).TopAuthor);

        await FinishOnAsync(sophie, "Yellowface", new DateTime(LastYear, 2, 3), author: "R.F. Kuang");

        Assert.Equal(
            new AuthorCountResult("R.F. Kuang", 2),
            (await YearAsync(sophie, LastYear)).TopAuthor
        );
    }

    [Fact]
    public async Task ReadingABookAgainCountsAsAReread()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var babel = await FinishOnAsync(sophie, "Babel", new DateTime(LastYear - 1, 5, 1));
        await FinishAgainOnAsync(sophie, babel, new DateTime(LastYear, 5, 1));

        var year = await YearAsync(sophie, LastYear);

        Assert.Equal(1, year.BooksRead);
        Assert.Equal(1, year.Rereads);
        Assert.Equal(0, (await YearAsync(sophie, LastYear - 1)).Rereads);
    }

    [Fact]
    public async Task AQuietYearIsAllZeros()
    {
        var sophie = await _factory.SignInAsync("sophie");

        var year = await YearAsync(sophie, LastYear);

        Assert.Equal(0, year.BooksRead);
        Assert.Equal(0, year.PagesRead);
        Assert.Null(year.AverageRating);
        Assert.Null(year.Longest);
        Assert.Empty(year.Books);
    }

    [Fact]
    public async Task OnlyYourOwnReadingCounts()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var tom = await _factory.SignInAsync("tom");
        await FinishOnAsync(tom, "Babel", new DateTime(LastYear, 1, 1));

        Assert.Equal(0, (await YearAsync(sophie, LastYear)).BooksRead);
    }
}
