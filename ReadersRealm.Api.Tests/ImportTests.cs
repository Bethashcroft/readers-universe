using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ReadersRealm.Api.Import;
using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Tests;

public class ImportTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public ImportTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private const string Csv = """"""
        Book Id,Title,Author,Author l-f,Additional Authors,ISBN,ISBN13,My Rating,Publisher,Binding,Number of Pages,Year Published,Original Publication Year,Date Read,Date Added,Bookshelves,Bookshelves with positions,Exclusive Shelf,My Review,Spoiler,Private Notes,Read Count,Owned Copies
        221954702,The One Night Stand,L.H. Stacey,"Stacey, L.H.",,"=""1785138723""","=""9781785138720""",2.0,Boldwood Books,Kindle Edition,281,2026,,2026/07/26,2026/07/24,,,read,"Shite. Feels like a first draft.",true,,1,0
        44107392,What You Did,Claire McGowan,"McGowan, Claire",,"=""1542091349""","=""9781542091343""",0.0,Thomas & Mercer,Kindle Edition,282,2019,2019,2026/07/19,2026/06/25,,,read,Feel a bit weird rating this,,,1,0
        221207871,The Profiler,Helen Sarah Fields,"Fields, Helen Sarah",Jess Nesling,"=""""","=""""",3.0,Avon,Audible Audio,11,2024,2024,2026/04/22,2026/03/26,,,read,Boring ending but a great plot,,,1,0
        61897968,"Practice Makes Perfect (When in Rome, #2)",Sarah       Adams,"Adams, Sarah",,"=""0593500806""","=""9780593500804""",1.0,Dell,Paperback,335,2023,2023,2025/08/20,2023/09/18,,,read,"DNF page 30.<br/><br/>Cba with this",,,1,0
        60811826,I Who Have Never Known Men,Jacqueline Harpman,"Harpman, Jacqueline",,"=""1945492600""","=""9781945492600""",0,Transit Books,Paperback,173,2022,1995,,2025/02/09,to-read,to-read (#31),to-read,,,,0,0
        """""";

    private static MultipartFormDataContent FileFor(string csv)
    {
        var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        content.Add(bytes, "file", "goodreads_library_export.csv");
        return content;
    }

    private async Task<ImportSummaryResult> ImportAsync(HttpClient client, bool preview = false)
    {
        var response = await client.PostAsync(
            $"/api/library/import?preview={preview.ToString().ToLowerInvariant()}",
            FileFor(Csv)
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImportSummaryResult>())!;
    }

    [Fact]
    public void ParserReadsGoodreadsQuirks()
    {
        var parser = new GoodreadsCsvParser();
        var result = parser.Parse(CsvReader.Parse(Csv));

        Assert.Null(result.Error);
        Assert.Equal(5, result.Books.Count);

        var stand = result.Books[0];
        Assert.Equal("9781785138720", stand.Isbn);
        Assert.Equal(2, stand.Rating);
        Assert.True(stand.ContainsSpoiler);
        Assert.Equal("read", stand.Shelf);
        Assert.Equal(new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc), stand.ReadDate);

        var noStars = result.Books[1];
        Assert.Null(noStars.Rating);
        Assert.Equal("Feel a bit weird rating this", noStars.ReviewText);

        var noIsbn = result.Books[2];
        Assert.Equal(string.Empty, noIsbn.Isbn);

        var doubleSpaced = result.Books[3];
        Assert.Equal("Sarah Adams", doubleSpaced.Author);
        Assert.Equal("DNF page 30.\n\nCba with this", doubleSpaced.ReviewText);

        var toRead = result.Books[4];
        Assert.Equal("want-to-read", toRead.Shelf);
        Assert.Null(toRead.Rating);
        Assert.Equal(string.Empty, toRead.ReviewText);
    }

    [Theory]
    [InlineData("read", "read")]
    [InlineData("currently-reading", "currently-reading")]
    [InlineData("to-read", "want-to-read")]
    [InlineData("did-not-finish", "dnf")]
    [InlineData("dnf", "dnf")]
    [InlineData("abandoned", "dnf")]
    [InlineData("tbr", "tbr")]
    [InlineData("some-custom-shelf", "want-to-read")]
    public void ShelfNamesMapOntoOurOwnShelves(string goodreads, string expected)
    {
        var csv =
            "Title,Author,ISBN,ISBN13,My Rating,Date Read,Date Added,Exclusive Shelf,My Review,Spoiler\n"
            + $"A Book,An Author,,,0,,2025/01/01,{goodreads},,";

        var result = new GoodreadsCsvParser().Parse(CsvReader.Parse(csv));

        Assert.Equal(expected, result.Books.Single().Shelf);
    }

    [Fact]
    public void CsvReaderKeepsCommasAndQuotesInsideFields()
    {
        var rows = CsvReader.Parse("a,b\n\"one, two\",\"he said \"\"hi\"\"\"");

        Assert.Equal(2, rows.Count);
        Assert.Equal("one, two", rows[1][0]);
        Assert.Equal("he said \"hi\"", rows[1][1]);
    }

    [Fact]
    public async Task PreviewReportsWhatWouldHappenWithoutWriting()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        var preview = await ImportAsync(_client, preview: true);

        Assert.Equal("Goodreads", preview.Service);
        Assert.False(preview.Committed);
        Assert.Equal(5, preview.RowsFound);
        Assert.Equal(5, preview.Added);
        Assert.Equal(4, preview.ReviewsAdded);

        var shelves = await _client.GetLibraryAsync();
        Assert.Empty(shelves!);
    }

    [Fact]
    public async Task ImportPutsBooksReviewsAndSpoilersOnTheShelves()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        var summary = await ImportAsync(_client);

        Assert.True(summary.Committed);
        Assert.Equal(5, summary.Added);
        Assert.Equal(5, summary.NewToCatalogue);
        Assert.Equal(4, summary.ReviewsAdded);
        Assert.Equal(4, summary.ByShelf["read"]);
        Assert.Equal(1, summary.ByShelf["want-to-read"]);

        var shelves = await _client.GetLibraryAsync();
        Assert.Equal(5, shelves!.Length);

        var stand = shelves.Single(b => b.Title == "The One Night Stand");
        Assert.Equal(2, stand.Rating);

        var reviews = await _client.GetFromJsonAsync<ReviewResult[]>(
            $"/api/reviews/book/{stand.BookId}"
        );
        Assert.Equal("Shite. Feels like a first draft.", reviews!.Single().Text);
    }

    [Fact]
    public async Task ImportKeepsReviewsThatHaveNoRatingOutOfTheAverage()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        await ImportAsync(_client);

        var shelves = await _client.GetLibraryAsync();
        var noStars = shelves!.Single(b => b.Title == "What You Did");

        Assert.Null(noStars.Rating);

        var detail = await _client.GetFromJsonAsync<BookDetailResult>(
            $"/api/books/{noStars.BookId}"
        );

        Assert.Null(detail!.AverageRating);
        Assert.Equal(0, detail.RatingCount);

        var reviews = await _client.GetFromJsonAsync<ReviewResult[]>(
            $"/api/reviews/book/{noStars.BookId}"
        );
        Assert.Equal("Feel a bit weird rating this", reviews!.Single().Text);
        Assert.Null(reviews.Single().Rating);
    }

    [Fact]
    public async Task ImportingTwiceDoesNotDuplicateAnything()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        await ImportAsync(_client);
        var second = await ImportAsync(_client);

        Assert.Equal(0, second.Added);
        Assert.Equal(5, second.AlreadyOnShelves);
        Assert.Equal(0, second.NewToCatalogue);

        var shelves = await _client.GetLibraryAsync();
        Assert.Equal(5, shelves!.Length);
    }

    [Fact]
    public async Task ImportReusesCatalogueBooksSomeoneElseAlreadyAdded()
    {
        var samClient = _factory.CreateClient();
        var sam = await samClient.RegisterAsync("sam");
        samClient.Authenticate(sam.Token);
        await samClient.AddBookAsync("The Profiler", author: "Helen Sarah Fields");

        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        var summary = await ImportAsync(_client);

        Assert.Equal(5, summary.Added);
        Assert.Equal(4, summary.NewToCatalogue);
    }

    [Fact]
    public async Task RejectsAFileThatIsNotAGoodreadsExport()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("name,age\nbeth,27")), "file", "wrong.csv");

        var response = await _client.PostAsync("/api/library/import", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LibraryPagesResultsAndFiltersByShelfAcrossTheWholeLibrary()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        await ImportAsync(_client);

        var firstPage = await _client.GetLibraryPageAsync("?pageSize=2");

        Assert.Equal(2, firstPage.Items.Length);
        Assert.Equal(5, firstPage.Total);
        Assert.Equal(3, firstPage.TotalPages);

        var lastPage = await _client.GetLibraryPageAsync("?pageSize=2&page=3");
        Assert.Single(lastPage.Items);

        var allTitles = new List<string>();
        for (var p = 1; p <= firstPage.TotalPages; p++)
        {
            allTitles.AddRange(
                (await _client.GetLibraryPageAsync($"?pageSize=2&page={p}")).Items.Select(b =>
                    b.Title
                )
            );
        }
        Assert.Equal(5, allTitles.Distinct().Count());

        // The shelf filter must run over the whole library, not just one page.
        var wishlist = await _client.GetLibraryPageAsync("?shelf=want-to-read&pageSize=2");
        Assert.Equal(1, wishlist.Total);
        Assert.Equal("I Who Have Never Known Men", wishlist.Items.Single().Title);
    }

    [Fact]
    public async Task SearchingYourShelvesLooksAtEveryPageNotJustTheCurrentOne()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        await ImportAsync(_client);

        var byTitle = await _client.GetLibraryPageAsync("?search=profiler&pageSize=2");
        Assert.Equal(1, byTitle.Total);
        Assert.Equal("The Profiler", byTitle.Items.Single().Title);

        var byAuthor = await _client.GetLibraryPageAsync("?search=mcgowan");
        Assert.Equal("What You Did", byAuthor.Items.Single().Title);

        var caseInsensitive = await _client.GetLibraryPageAsync("?search=PROFILER");
        Assert.Equal(1, caseInsensitive.Total);

        var withShelf = await _client.GetLibraryPageAsync("?search=a&shelf=want-to-read");
        Assert.All(withShelf.Items, b => Assert.Equal("want-to-read", b.Shelf));
    }

    [Fact]
    public async Task SortingOrdersTheWholeLibraryNotJustThePage()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        await ImportAsync(_client);

        var byTitle = await _client.GetLibraryPageAsync("?sort=title");
        Assert.Equal(
            byTitle.Items.Select(b => b.Title).OrderBy(t => t, StringComparer.Ordinal),
            byTitle.Items.Select(b => b.Title)
        );

        var byAuthor = await _client.GetLibraryPageAsync("?sort=author");
        Assert.Equal("Claire McGowan", byAuthor.Items.First().Author);

        var byRating = await _client.GetLibraryPageAsync("?sort=rating");
        Assert.Equal(3, byRating.Items.First().Rating);
        Assert.Null(byRating.Items.Last().Rating);

        var firstPage = await _client.GetLibraryPageAsync("?sort=title&pageSize=2&page=1");
        var secondPage = await _client.GetLibraryPageAsync("?sort=title&pageSize=2&page=2");
        Assert.Empty(
            firstPage.Items.Select(b => b.Title).Intersect(secondPage.Items.Select(b => b.Title))
        );
    }

    [Fact]
    public async Task LibraryRejectsASortThatDoesNotExist()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        var response = await _client.GetAsync("/api/library?sort=whatever");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LibraryRejectsAShelfThatDoesNotExist()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);

        var response = await _client.GetAsync("/api/library?shelf=made-up");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShelfCountsCoverTheWholeLibrary()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        await ImportAsync(_client);

        var counts = await _client.GetFromJsonAsync<Dictionary<string, int>>(
            "/api/library/shelf-counts"
        );

        Assert.Equal(4, counts!["read"]);
        Assert.Equal(1, counts["want-to-read"]);
    }

    [Fact]
    public async Task CoverBackfillSettlesAndDoesNotRepeatWorkOnASecondRun()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        await ImportAsync(_client);

        var firstRunChecked = 0;
        var afterId = 0;

        for (var batch = 0; batch < 20; batch++)
        {
            var response = await _client.PostAsync(
                $"/api/library/refresh-covers?afterId={afterId}&max=2&withTotal=true",
                null
            );
            response.EnsureSuccessStatusCode();
            var result = (await response.Content.ReadFromJsonAsync<CoverBackfillResult>())!;

            firstRunChecked += result.Checked;
            afterId = result.NextAfterId;

            if (result.Done || result.Checked == 0)
            {
                break;
            }
        }

        Assert.Equal(5, firstRunChecked);

        var second = await _client.PostAsync("/api/library/refresh-covers?afterId=0&withTotal=true", null);
        second.EnsureSuccessStatusCode();
        var settled = (await second.Content.ReadFromJsonAsync<CoverBackfillResult>())!;

        Assert.Equal(0, settled.Total);
        Assert.Equal(0, settled.Checked);
        Assert.True(settled.Done);
    }

    [Fact]
    public async Task AFoundCoverIsConfirmedAndAppliedToTheBook()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        await ImportAsync(_client);

        var source = (FakeCoverSource)_factory.Services.GetRequiredService<ICoverSource>();
        source.Result = CoverResult.Found("https://covers.openlibrary.org/b/id/42-L.jpg");
        _factory.Handler.Status = System.Net.HttpStatusCode.OK;
        _factory.Handler.ContentLength = 5000;

        var run = await _client.PostAsync(
            "/api/library/refresh-covers?afterId=0&withTotal=true",
            null
        );
        run.EnsureSuccessStatusCode();
        var result = (await run.Content.ReadFromJsonAsync<CoverBackfillResult>())!;

        Assert.Equal(1, result.Fixed);
        Assert.Equal(4, result.AlreadyFine);

        var shelves = await _client.GetLibraryAsync();
        var noIsbn = shelves.Single(b => b.Title == "The Profiler");
        Assert.Equal("https://covers.openlibrary.org/b/id/42-L.jpg", noIsbn.CoverUrl);
    }

    [Fact]
    public async Task AThrottledSourceStopsTheRunInsteadOfHammeringOn()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        await ImportAsync(_client);

        var source = (FakeCoverSource)_factory.Services.GetRequiredService<ICoverSource>();
        source.Result = CoverResult.Throttled;

        var run = await _client.PostAsync(
            "/api/library/refresh-covers?afterId=0&withTotal=true",
            null
        );
        var result = (await run.Content.ReadFromJsonAsync<CoverBackfillResult>())!;

        Assert.True(result.Throttled);
        Assert.True(result.Done);
        Assert.Equal(0, result.NotFound);
    }

    [Fact]
    public async Task AnUnreachableCoverSourceLeavesBooksAloneRatherThanGivingUpOnThem()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        await ImportAsync(_client);

        var source = (FakeCoverSource)
            _factory.Services.GetRequiredService<ReadersRealm.Api.Services.ICoverSource>();
        source.Result = ReadersRealm.Api.Services.CoverResult.Unavailable;

        var run = await _client.PostAsync(
            "/api/library/refresh-covers?afterId=0&withTotal=true",
            null
        );
        run.EnsureSuccessStatusCode();
        var result = (await run.Content.ReadFromJsonAsync<CoverBackfillResult>())!;

        Assert.Equal(0, result.NotFound);
        Assert.True(result.Unreachable > 0);

        source.Result = ReadersRealm.Api.Services.CoverResult.NothingThere;

        var retry = await _client.PostAsync(
            "/api/library/refresh-covers?afterId=0&withTotal=true",
            null
        );
        var second = (await retry.Content.ReadFromJsonAsync<CoverBackfillResult>())!;

        Assert.True(
            second.Total > 0,
            "Books skipped because the source was unreachable must still be candidates."
        );
    }

    [Fact]
    public async Task ReimportingDoesNotUndoASettledCoverDecision()
    {
        var beth = await _client.RegisterAsync("beth");
        _client.Authenticate(beth.Token);
        await ImportAsync(_client);

        var first = await _client.PostAsync(
            "/api/library/refresh-covers?afterId=0&withTotal=true",
            null
        );
        first.EnsureSuccessStatusCode();

        var settled = await _client.PostAsync(
            "/api/library/refresh-covers?afterId=0&withTotal=true",
            null
        );
        Assert.Equal(
            0,
            (await settled.Content.ReadFromJsonAsync<CoverBackfillResult>())!.Total
        );

        await ImportAsync(_client);

        var afterReimport = await _client.PostAsync(
            "/api/library/refresh-covers?afterId=0&withTotal=true",
            null
        );
        var again = (await afterReimport.Content.ReadFromJsonAsync<CoverBackfillResult>())!;

        Assert.Equal(0, again.Total);
    }

    [Fact]
    public async Task ImportRequiresSigningIn()
    {
        var response = await _client.PostAsync("/api/library/import", FileFor(Csv));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
