using System.Text.Json;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public class OpenLibraryBookLookup : IBookLookup
{
    private readonly HttpClient _http;

    public OpenLibraryBookLookup(HttpClient http)
    {
        _http = http;
    }

    public async Task<BookLookupResult?> LookupAsync(string isbn)
    {
        var clean = Book.CleanIsbn(isbn);
        if (string.IsNullOrEmpty(clean))
        {
            return null;
        }

        using var doc = await ReadJsonAsync(
            $"https://openlibrary.org/api/books?bibkeys=ISBN:{clean}&format=json&jscmd=data"
        );

        if (doc == null || !doc.RootElement.TryGetProperty($"ISBN:{clean}", out var book))
        {
            return null;
        }

        var title = GetString(book, "title") ?? "";
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var author = "";
        if (
            book.TryGetProperty("authors", out var authors)
            && authors.ValueKind == JsonValueKind.Array
        )
        {
            author = string.Join(
                ", ",
                authors
                    .EnumerateArray()
                    .Select(a => GetString(a, "name"))
                    .Where(n => !string.IsNullOrWhiteSpace(n))
            );
        }

        var cover = "";
        if (book.TryGetProperty("cover", out var c))
        {
            cover =
                GetString(c, "large")
                ?? GetString(c, "medium")
                ?? GetString(c, "small")
                ?? "";
        }
        if (string.IsNullOrEmpty(cover))
        {
            cover = $"https://covers.openlibrary.org/b/isbn/{clean}-L.jpg?default=false";
        }

        return new BookLookupResult(title, author, cover, PositiveInt(book, "number_of_pages"));
    }

    public async Task<int?> FindPageCountAsync(string title, string author)
    {
        var bareTitle = OpenLibraryMatching.BareTitle(title);
        if (bareTitle.Length == 0)
        {
            return null;
        }

        var url =
            $"https://openlibrary.org/search.json?title={Uri.EscapeDataString(bareTitle)}"
            + $"&author={Uri.EscapeDataString(author)}"
            + "&limit=5&fields=title,author_name,number_of_pages_median";

        using var doc = await ReadJsonAsync(url);

        if (
            doc == null
            || !doc.RootElement.TryGetProperty("docs", out var docs)
            || docs.ValueKind != JsonValueKind.Array
        )
        {
            return null;
        }

        var wantedTitle = OpenLibraryMatching.Normalise(bareTitle);
        var wantedAuthor = OpenLibraryMatching.Normalise(author);

        foreach (var candidate in docs.EnumerateArray())
        {
            if (!OpenLibraryMatching.TitleMatches(candidate, wantedTitle))
            {
                continue;
            }

            if (
                wantedAuthor.Length > 0
                && !OpenLibraryMatching.AuthorMatches(candidate, wantedAuthor)
            )
            {
                continue;
            }

            var pages = PositiveInt(candidate, "number_of_pages_median");
            if (pages != null)
            {
                return pages;
            }
        }

        return null;
    }

    public async Task<List<BookSearchResult>> SearchAsync(string query)
    {
        var url =
            $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(query)}"
            + "&limit=8&fields=title,author_name,cover_i,isbn,first_publish_year,number_of_pages_median";

        using var doc = await ReadJsonAsync(url);
        var results = new List<BookSearchResult>();

        if (
            doc == null
            || !doc.RootElement.TryGetProperty("docs", out var docs)
            || docs.ValueKind != JsonValueKind.Array
        )
        {
            return results;
        }

        foreach (var candidate in docs.EnumerateArray())
        {
            var title = GetString(candidate, "title");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            results.Add(
                new BookSearchResult(
                    title,
                    JoinNames(candidate, "author_name"),
                    CoverFor(candidate),
                    FirstIsbn(candidate),
                    PositiveInt(candidate, "number_of_pages_median"),
                    PositiveInt(candidate, "first_publish_year")
                )
            );
        }

        return results;
    }

    private static string JoinNames(JsonElement element, string property) =>
        element.TryGetProperty(property, out var names) && names.ValueKind == JsonValueKind.Array
            ? string.Join(
                ", ",
                names
                    .EnumerateArray()
                    .Select(n => n.GetString())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
            )
            : "";

    private static string CoverFor(JsonElement doc) =>
        doc.TryGetProperty("cover_i", out var id) && id.TryGetInt64(out var coverId)
            ? $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg"
            : "";

    private static string FirstIsbn(JsonElement doc)
    {
        if (!doc.TryGetProperty("isbn", out var isbns) || isbns.ValueKind != JsonValueKind.Array)
        {
            return "";
        }

        var all = isbns
            .EnumerateArray()
            .Select(i => i.GetString() ?? "")
            .Where(i => i.Length > 0)
            .ToList();

        return all.FirstOrDefault(i => i.Length == 13) ?? all.FirstOrDefault() ?? "";
    }

    private async Task<JsonDocument?> ReadJsonAsync(string url)
    {
        try
        {
            using var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
            when (ex
                    is HttpRequestException
                        or OperationCanceledException
                        or IOException
                        or JsonException
            )
        {
            return null;
        }
    }

    private static int? PositiveInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var pages)
        && pages.ValueKind == JsonValueKind.Number
        && pages.TryGetInt32(out var parsed)
        && parsed > 0
            ? parsed
            : null;

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
