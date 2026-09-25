using System.Net;
using System.Text.Json;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public class GoogleBooksCoverSource(HttpClient http, IConfiguration configuration) : ICoverSource
{
    public const string KeySetting = "GoogleBooks:ApiKey";

    public const string Host = "books.google.com";

    public const string KeyHeader = "X-Goog-Api-Key";

    private const string LargerImage = "fife=w400-h600";

    private readonly HttpClient _http = http;
    private readonly string _apiKey = configuration[KeySetting] ?? string.Empty;

    public async Task<CoverResult> FindCoverAsync(
        string title,
        string author,
        string isbn,
        CancellationToken cancellationToken
    )
    {
        var clean = Book.CleanIsbn(isbn);
        var worst = CoverLookup.NothingThere;

        if (clean.Length > 0)
        {
            var byIsbn = await SearchAsync(
                $"isbn:{clean}",
                volume => HasIsbn(volume, clean),
                cancellationToken
            );

            if (byIsbn.Outcome is CoverLookup.Found or CoverLookup.Throttled)
            {
                return byIsbn;
            }

            worst = CoverResult.Worse(worst, byIsbn.Outcome);
        }

        var bareTitle = OpenLibraryMatching.BareTitle(title).Replace("\"", "");
        var plainAuthor = author.Replace("\"", "");
        var wantedTitle = OpenLibraryMatching.Normalise(bareTitle);
        var wantedAuthor = OpenLibraryMatching.Normalise(plainAuthor);

        if (wantedTitle.Length == 0)
        {
            return CoverResult.From(worst);
        }

        var query =
            wantedAuthor.Length > 0
                ? $"intitle:\"{bareTitle}\" inauthor:\"{plainAuthor}\""
                : $"intitle:\"{bareTitle}\"";

        var bySearch = await SearchAsync(
            query,
            volume =>
                OpenLibraryMatching.TitleMatches(volume, wantedTitle)
                && (wantedAuthor.Length == 0 || AuthorMatches(volume, wantedAuthor)),
            cancellationToken
        );

        return bySearch.Outcome == CoverLookup.Found
            ? bySearch
            : CoverResult.From(CoverResult.Worse(worst, bySearch.Outcome));
    }

    public static string? CoverFrom(JsonElement volume)
    {
        if (
            !volume.TryGetProperty("imageLinks", out var links)
            || links.ValueKind != JsonValueKind.Object
            || !links.TryGetProperty("thumbnail", out var thumbnail)
            || thumbnail.ValueKind != JsonValueKind.String
            || !Uri.TryCreate(thumbnail.GetString(), UriKind.Absolute, out var uri)
            || uri.Host != Host
        )
        {
            return null;
        }

        var query = uri
            .Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => part != "edge=curl" && !part.StartsWith("fife="))
            .Append(LargerImage);

        return new UriBuilder(uri)
        {
            Scheme = Uri.UriSchemeHttps,
            Port = -1,
            Query = string.Join('&', query),
        }.Uri.ToString();
    }

    private async Task<CoverResult> SearchAsync(
        string query,
        Func<JsonElement, bool> matches,
        CancellationToken cancellationToken
    )
    {
        var url =
            $"https://www.googleapis.com/books/v1/volumes?q={Uri.EscapeDataString(query)}"
            + "&maxResults=5&printType=books"
            + "&fields=items(volumeInfo(title,authors,industryIdentifiers,imageLinks))";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add(KeyHeader, _apiKey);
            using var response = await _http.SendAsync(request, cancellationToken);

            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.Forbidden)
            {
                return CoverResult.Throttled;
            }

            if (!response.IsSuccessStatusCode)
            {
                return CoverResult.Unavailable;
            }

            using var json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken)
            );

            if (
                !json.RootElement.TryGetProperty("items", out var items)
                || items.ValueKind != JsonValueKind.Array
            )
            {
                return CoverResult.NothingThere;
            }

            foreach (var item in items.EnumerateArray())
            {
                if (
                    item.TryGetProperty("volumeInfo", out var volume)
                    && matches(volume)
                    && CoverFrom(volume) is { } cover
                )
                {
                    return CoverResult.Found(cover);
                }
            }

            return CoverResult.NothingThere;
        }
        catch (Exception ex)
            when (ex
                    is HttpRequestException
                        or OperationCanceledException
                        or IOException
                        or JsonException
            )
        {
            return CoverResult.Unavailable;
        }
    }

    private static bool HasIsbn(JsonElement volume, string isbn) =>
        volume.TryGetProperty("industryIdentifiers", out var identifiers)
        && identifiers.ValueKind == JsonValueKind.Array
        && identifiers
            .EnumerateArray()
            .Any(id =>
                id.TryGetProperty("identifier", out var value)
                && Book.CleanIsbn(value.GetString() ?? string.Empty) == isbn
            );

    private static bool AuthorMatches(JsonElement volume, string wantedAuthor) =>
        volume.TryGetProperty("authors", out var authors)
        && authors.ValueKind == JsonValueKind.Array
        && authors
            .EnumerateArray()
            .Any(name => OpenLibraryMatching.Normalise(name.GetString() ?? string.Empty) == wantedAuthor);
}
