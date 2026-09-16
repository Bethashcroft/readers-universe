using System.Text.Json;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public class OpenLibraryCoverSource(HttpClient http) : ICoverSource
{
    private readonly HttpClient _http = http;

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
            var byIsbn = await CoverForIsbnAsync(clean, cancellationToken);

            if (byIsbn.Outcome == CoverLookup.Found)
            {
                return byIsbn;
            }

            if (byIsbn.Outcome == CoverLookup.Throttled)
            {
                return CoverResult.Throttled;
            }

            worst = CoverResult.Worse(worst, byIsbn.Outcome);
        }

        var bySearch = await CoverBySearchAsync(title, author, cancellationToken);

        if (bySearch.Outcome == CoverLookup.Found)
        {
            return bySearch;
        }

        return CoverResult.Worse(worst, bySearch.Outcome) switch
        {
            CoverLookup.Throttled => CoverResult.Throttled,
            CoverLookup.Unavailable => CoverResult.Unavailable,
            _ => CoverResult.NothingThere,
        };
    }

    private async Task<CoverResult> CoverForIsbnAsync(
        string isbn,
        CancellationToken cancellationToken
    )
    {
        var key = $"ISBN:{isbn}";
        var (json, reach) = await ReadJsonAsync(
            $"https://openlibrary.org/api/books?bibkeys={Uri.EscapeDataString(key)}&format=json&jscmd=data",
            cancellationToken
        );

        if (reach != Reach.Answered)
        {
            return reach == Reach.Throttled ? CoverResult.Throttled : CoverResult.Unavailable;
        }

        if (json == null)
        {
            return CoverResult.NothingThere;
        }

        using (json)
        {
            if (
                json.RootElement.TryGetProperty(key, out var entry)
                && entry.TryGetProperty("cover", out var cover)
            )
            {
                foreach (var size in new[] { "large", "medium", "small" })
                {
                    if (
                        cover.TryGetProperty(size, out var image)
                        && image.ValueKind == JsonValueKind.String
                        && image.GetString() is { Length: > 0 } found
                    )
                    {
                        return CoverResult.Found(found);
                    }
                }
            }
        }

        return CoverResult.NothingThere;
    }

    private async Task<CoverResult> CoverBySearchAsync(
        string title,
        string author,
        CancellationToken cancellationToken
    )
    {
        var bareTitle = OpenLibraryMatching.BareTitle(title);
        var terms = $"{bareTitle} {author}".Trim();

        if (terms.Length == 0)
        {
            return CoverResult.NothingThere;
        }

        var (json, reach) = await ReadJsonAsync(
            $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(terms)}&limit=5&fields=cover_i,title,author_name",
            cancellationToken
        );

        if (reach != Reach.Answered)
        {
            return reach == Reach.Throttled ? CoverResult.Throttled : CoverResult.Unavailable;
        }

        if (json == null)
        {
            return CoverResult.NothingThere;
        }

        using (json)
        {
            if (!json.RootElement.TryGetProperty("docs", out var docs))
            {
                return CoverResult.NothingThere;
            }

            var wantedTitle = OpenLibraryMatching.Normalise(bareTitle);
            var wantedAuthor = OpenLibraryMatching.Normalise(author);

            foreach (var doc in docs.EnumerateArray())
            {
                if (!doc.TryGetProperty("cover_i", out var id) || !id.TryGetInt64(out var coverId))
                {
                    continue;
                }

                if (!OpenLibraryMatching.TitleMatches(doc, wantedTitle))
                {
                    continue;
                }

                if (
                    wantedAuthor.Length > 0
                    && !OpenLibraryMatching.AuthorMatches(doc, wantedAuthor)
                )
                {
                    continue;
                }

                return CoverResult.Found($"https://covers.openlibrary.org/b/id/{coverId}-L.jpg");
            }
        }

        return CoverResult.NothingThere;
    }

    private enum Reach
    {
        Answered,
        Unavailable,
        Throttled,
    }

    private async Task<(JsonDocument? Json, Reach Reach)> ReadJsonAsync(
        string url,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (null, Reach.Answered);
            }

            if (
                response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                || response.StatusCode == System.Net.HttpStatusCode.Forbidden
            )
            {
                return (null, Reach.Throttled);
            }

            if (!response.IsSuccessStatusCode)
            {
                return (null, Reach.Unavailable);
            }

            return (
                JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)),
                Reach.Answered
            );
        }
        catch (Exception ex)
            when (ex
                    is HttpRequestException
                        or OperationCanceledException
                        or IOException
                        or JsonException
            )
        {
            return (null, Reach.Unavailable);
        }
    }
}
