using System.Net;
using System.Net.Http.Json;
using System.Text;
using ReadersRealm.Api.Import;

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

        var shelves = await _client.GetFromJsonAsync<BookResult[]>("/api/library");
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

        var shelves = await _client.GetFromJsonAsync<BookResult[]>("/api/library");
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

        var shelves = await _client.GetFromJsonAsync<BookResult[]>("/api/library");
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

        var shelves = await _client.GetFromJsonAsync<BookResult[]>("/api/library");
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
    public async Task ImportRequiresSigningIn()
    {
        var response = await _client.PostAsync("/api/library/import", FileFor(Csv));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
