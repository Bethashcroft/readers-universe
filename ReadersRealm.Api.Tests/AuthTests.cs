using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class AuthTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public AuthTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                userName = "alice",
                email = "alice@example.com",
                displayName = "Alice",
                password = "Password1",
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResult>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("alice", body.UserName);
    }
}