using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record AuthResult(string Token, string UserId, string UserName, string DisplayName);

public record BookResult(
    int Id,
    string Title,
    string Author,
    string CoverUrl,
    string Shelf,
    string Offer,
    int? Rating,
    string UserId,
    string OwnerName
);

public record BorrowResult(int Id, int BookId, string Status, string FromUserName);

public record ReviewResult(
    int Id,
    int Rating,
    string Text,
    int BookId,
    string UserId,
    string UserName
);

public static class ApiTestExtensions
{
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
        string offer = "none"
    )
    {
        var response = await client.PostAsJsonAsync(
            "/api/books",
            new
            {
                title,
                author = "Test Author",
                coverUrl = "x",
                shelf,
                offer,
                rating = (int?)null,
            }
        );

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookResult>())!;
    }

        public static async Task<BorrowResult> RequestBookAsync(
        this HttpClient client,
        int bookId,
        string message = ""
    )
    {
        var response = await client.PostAsJsonAsync(
            "/api/borrowrequests",
            new { bookId, message }
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