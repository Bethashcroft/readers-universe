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
    int? Rating,
    string UserId
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
        string shelf = "available-to-borrow"
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
                rating = (int?)null,
            }
        );

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookResult>())!;
    }
}