using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record AuthResult(string Token, string UserId, string UserName, string DisplayName);

public record BookResult(
    int Id,
    int BookId,
    string Title,
    string Author,
    string CoverUrl,
    string Isbn,
    string Shelf,
    string Offer,
    int? Rating,
    string UserId,
    string OwnerName,
    bool CanRequest
);

public record BookOwnerResult(
    int LibraryEntryId,
    string UserName,
    string DisplayName,
    string Offer
);

public record BookDetailResult(
    int Id,
    string Title,
    string Author,
    string Isbn,
    double? AverageRating,
    int RatingCount,
    BookResult? MyEntry,
    BookOwnerResult[] Owners
);

public record BorrowResult(int Id, int BookId, string Status, string FromUserName);

public record ReviewResult(
    int Id,
    int? Rating,
    string Text,
    int BookId,
    string UserId,
    string UserName
);

public record ImportSummaryResult(
    string Service,
    bool Committed,
    int RowsFound,
    int Added,
    int AlreadyOnShelves,
    int ReviewsAdded,
    int NewToCatalogue,
    int SkippedRows,
    Dictionary<string, int> ByShelf,
    string[] Sample
);

public record Paged<T>(T[] Items, int Page, int PageSize, int Total, int TotalPages);


public static class ApiTestExtensions
{
    public static async Task<Paged<BookResult>> GetLibraryPageAsync(
        this HttpClient client,
        string query = ""
    ) => (await client.GetFromJsonAsync<Paged<BookResult>>($"/api/library{query}"))!;

    public static async Task<BookResult[]> GetLibraryAsync(
        this HttpClient client,
        string query = ""
    ) => (await client.GetLibraryPageAsync(query)).Items;

    public static async Task<Paged<BookResult>> GetBrowsePageAsync(
        this HttpClient client,
        string query = ""
    ) => (await client.GetFromJsonAsync<Paged<BookResult>>($"/api/books/browse{query}"))!;

    public static async Task<BookResult[]> GetBrowseAsync(
        this HttpClient client,
        string query = ""
    ) => (await client.GetBrowsePageAsync(query)).Items;

    public static async Task<AuthResult> RegisterAsync(this HttpClient client, string userName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                userName,
                email = $"{userName}@example.com",
                displayName = userName,
                password = "Password1",
            }
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResult>())!;
    }

    public static void Authenticate(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
    }

    public static async Task<BookResult> AddBookAsync(
        this HttpClient client,
        string title,
        string shelf = "read",
        string offer = "none",
        string author = "Test Author",
        int? rating = null,
        string isbn = ""
    )
    {
        var response = await client.PostAsJsonAsync(
            "/api/library",
            new
            {
                title,
                author,
                coverUrl = "x",
                isbn,
                shelf,
                offer,
                rating,
            }
        );

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookResult>())!;
    }

    public static async Task TrustAsync(this HttpClient client, string username)
    {
        var response = await client.PostAsync($"/api/users/{username}/trust", null);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<BorrowResult> RequestBookAsync(
        this HttpClient client,
        int libraryEntryId,
        string message = ""
    )
    {
        var response = await client.PostAsJsonAsync(
            "/api/borrowrequests",
            new { libraryEntryId, message }
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BorrowResult>())!;
    }

    public static async Task<ReviewResult> AddReviewAsync(
        this HttpClient client,
        int bookId,
        int rating = 4,
        string text = "A fine read"
    )
    {
        var response = await client.PostAsJsonAsync(
            "/api/reviews",
            new { rating, text, bookId }
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReviewResult>())!;
    }
}