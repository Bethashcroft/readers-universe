using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Tests;

public class GoogleBooksCoverSourceTests
{
    private const string Thumbnail =
        "http://books.google.com/books/content?id=abc&printsec=frontcover&img=1&zoom=1&edge=curl&source=gbs_api";

    private const string Cover =
        "https://books.google.com/books/content?id=abc&printsec=frontcover&img=1&zoom=1&source=gbs_api&fife=w400-h600";

    private sealed class ScriptedHandler(Func<Uri, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        public List<string> Keys { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request.RequestUri!);
            Keys.Add(
                request.Headers.TryGetValues(GoogleBooksCoverSource.KeyHeader, out var keys)
                    ? keys.Single()
                    : ""
            );
            return Task.FromResult(respond(request.RequestUri!));
        }
    }

    private static (GoogleBooksCoverSource Source, ScriptedHandler Handler) Answering(
        Func<Uri, HttpResponseMessage> respond
    )
    {
        var handler = new ScriptedHandler(respond);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { [GoogleBooksCoverSource.KeySetting] = "test-key" }
            )
            .Build();

        return (new GoogleBooksCoverSource(new HttpClient(handler), configuration), handler);
    }

    private static HttpResponseMessage Items(params string[] volumes) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""{"items":[{{string.Join(',', volumes)}}]}"""),
        };

    private static string Volume(
        string title,
        string author,
        string isbn = "9780000000000",
        bool withCover = true
    ) =>
        JsonSerializer.Serialize(
            new
            {
                volumeInfo = new
                {
                    title,
                    authors = new[] { author },
                    industryIdentifiers = new[] { new { type = "ISBN_13", identifier = isbn } },
                    imageLinks = withCover ? new { thumbnail = Thumbnail } : null,
                },
            }
        );

    private static bool IsIsbnSearch(Uri uri) => uri.Query.Contains("q=isbn%3A");

    [Fact]
    public async Task FindsACoverByIsbnOverHttpsWithoutThePageCurl()
    {
        var (source, handler) = Answering(_ => Items(Volume("Babel", "R. F. Kuang", "9780008501815")));

        var result = await source.FindCoverAsync("Babel", "R.F. Kuang", "978-0-00-850181-5", default);

        Assert.Equal(CoverLookup.Found, result.Outcome);
        Assert.Equal(Cover, result.Url);
        var request = Assert.Single(handler.Requests);
        Assert.Contains("q=isbn%3A9780008501815", request.Query);
        Assert.DoesNotContain("test-key", request.ToString());
        Assert.Equal("test-key", Assert.Single(handler.Keys));
    }

    [Fact]
    public async Task AnIsbnHitForADifferentBookIsIgnored()
    {
        var (source, _) = Answering(uri =>
            IsIsbnSearch(uri) ? Items(Volume("Babel", "R. F. Kuang", "9781111111111")) : Items()
        );

        var result = await source.FindCoverAsync("Babel", "R.F. Kuang", "9780008501815", default);

        Assert.Equal(CoverLookup.NothingThere, result.Outcome);
    }

    [Fact]
    public async Task ASearchHitNeedsTheSameTitleAndAuthor()
    {
        var (wrong, _) = Answering(_ =>
            Items(Volume("Babel", "Someone Else"), Volume("Babel Tower", "R. F. Kuang"))
        );
        var (right, handler) = Answering(_ => Items(Volume("Babel", "R. F. Kuang")));

        var missed = await wrong.FindCoverAsync("Babel (Standalone)", "R.F. Kuang", "", default);
        var found = await right.FindCoverAsync("Babel (Standalone)", "R.F. Kuang", "", default);

        Assert.Equal(CoverLookup.NothingThere, missed.Outcome);
        Assert.Equal(CoverLookup.Found, found.Outcome);
        Assert.Contains("intitle%3A%22Babel%22", Assert.Single(handler.Requests).Query);
    }

    [Fact]
    public async Task AVolumeWithoutACoverIsNotACover()
    {
        var (source, _) = Answering(_ => Items(Volume("Babel", "R. F. Kuang", withCover: false)));

        var result = await source.FindCoverAsync("Babel", "R.F. Kuang", "", default);

        Assert.Equal(CoverLookup.NothingThere, result.Outcome);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task BeingRateLimitedStopsStraightAway(HttpStatusCode status)
    {
        var (source, handler) = Answering(_ => new HttpResponseMessage(status));

        var result = await source.FindCoverAsync("Babel", "R.F. Kuang", "9780008501815", default);

        Assert.Equal(CoverLookup.Throttled, result.Outcome);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task AnOutageIsUnavailableNotNothingThere()
    {
        var (broken, _) = Answering(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var (offline, _) = Answering(_ => throw new HttpRequestException("offline"));

        Assert.Equal(
            CoverLookup.Unavailable,
            (await broken.FindCoverAsync("Babel", "R.F. Kuang", "", default)).Outcome
        );
        Assert.Equal(
            CoverLookup.Unavailable,
            (await offline.FindCoverAsync("Babel", "R.F. Kuang", "", default)).Outcome
        );
    }
}
