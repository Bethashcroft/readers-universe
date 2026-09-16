using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Tests;

public class FakeBookLookup : IBookLookup
{
    public const string KnownIsbn = "9780261103344";
    public const string UnknownIsbn = "0000000000";
    public const string KnownTitle = "Babel";

    public Task<int?> FindPageCountAsync(string title, string author) =>
        Task.FromResult<int?>(title == KnownTitle ? 569 : null);

    public Task<List<BookSearchResult>> SearchAsync(string query) =>
        Task.FromResult(
            query.Contains("hobbit", StringComparison.OrdinalIgnoreCase)
                ? new List<BookSearchResult>
                {
                    new(
                        "The Hobbit",
                        "J.R.R. Tolkien",
                        "https://covers.openlibrary.org/b/id/1-L.jpg",
                        KnownIsbn,
                        310,
                        1937
                    ),
                }
                : []
        );

    public Task<BookLookupResult?> LookupAsync(string isbn)
    {
        var clean = new string(isbn.Where(char.IsLetterOrDigit).ToArray());

        if (clean == KnownIsbn)
        {
            return Task.FromResult<BookLookupResult?>(
                new BookLookupResult(
                    "The Hobbit",
                    "J.R.R. Tolkien",
                    "https://covers.openlibrary.org/b/isbn/9780261103344-L.jpg",
                    310
                )
            );
        }

        return Task.FromResult<BookLookupResult?>(null);
    }
}
