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

        var url =
            $"https://openlibrary.org/api/books?bibkeys=ISBN:{clean}&format=json&jscmd=data";

        JsonDocument doc;
        try
        {
            using var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
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

        using var _ = doc;

        if (!doc.RootElement.TryGetProperty($"ISBN:{clean}", out var book))
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

        return new BookLookupResult(title, author, cover);
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
