using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Tests;

public class FakeBookLookup : IBookLookup
{
    public const string KnownIsbn = "9780261103344";
    public const string UnknownIsbn = "0000000000";

    public Task<BookLookupResult?> LookupAsync(string isbn)
    {
        var clean = new string(isbn.Where(char.IsLetterOrDigit).ToArray());

        if (clean == KnownIsbn)
        {
            return Task.FromResult<BookLookupResult?>(
                new BookLookupResult(
                    "The Hobbit",
                    "J.R.R. Tolkien",
                    "https://covers.openlibrary.org/b/isbn/9780261103344-L.jpg"
                )
            );
        }

        return Task.FromResult<BookLookupResult?>(null);
    }
}
