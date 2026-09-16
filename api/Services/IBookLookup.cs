namespace ReadersRealm.Api.Services;

public record BookLookupResult(string Title, string Author, string CoverUrl, int? PageCount);

public record BookSearchResult(
    string Title,
    string Author,
    string CoverUrl,
    string Isbn,
    int? PageCount,
    int? Year
);

public interface IBookLookup
{
    Task<BookLookupResult?> LookupAsync(string isbn);
    Task<int?> FindPageCountAsync(string title, string author);
    Task<List<BookSearchResult>> SearchAsync(string query);
}
