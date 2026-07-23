using System.Text.Json;

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
        var clean = new string(isbn.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrEmpty(clean))
        {
            return null;
        }

        var url =
            $"https://openlibrary.org/api/books?bibkeys=ISBN:{clean}&format=json&jscmd=data";

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        if (!doc.RootElement.TryGetProperty($"ISBN:{clean}", out var book))
        {
            return null;
        }

        var title = book.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
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
                    .Select(a => a.TryGetProperty("name", out var n) ? n.GetString() : null)
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
            cover = $"https://covers.openlibrary.org/b/isbn/{clean}-L.jpg";
        }

        return new BookLookupResult(title, author, cover);
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.GetString() : null;
}
