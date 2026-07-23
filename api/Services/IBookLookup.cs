namespace ReadersRealm.Api.Services;

public record BookLookupResult(string Title, string Author, string CoverUrl);

public interface IBookLookup
{
    Task<BookLookupResult?> LookupAsync(string isbn);
}
