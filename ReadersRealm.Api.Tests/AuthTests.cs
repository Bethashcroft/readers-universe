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

    private record ProfileResult(string UserName, string DisplayName, string VintedUrl);

    private Task<HttpResponseMessage> UpdateProfileAsync(string vintedUrl) =>
        _client.PutAsJsonAsync(
            "/api/auth/profile",
            new
            {
                displayName = "Alice",
                bio = "",
                vintedUrl,
            }
        );

    [Fact]
    public async Task UpdatingProfile_WithRealVintedUrl_Saves()
    {
        var user = await _client.RegisterAsync("alice");
        _client.Authenticate(user.Token);

        var response = await UpdateProfileAsync("https://www.vinted.co.uk/member/12345");

        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<ProfileResult>();
        Assert.Equal("https://www.vinted.co.uk/member/12345", profile!.VintedUrl);

        var cleared = await UpdateProfileAsync("");
        cleared.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task UpdatingProfile_WithNonVintedUrl_ReturnsBadRequest()
    {
        var user = await _client.RegisterAsync("alice");
        _client.Authenticate(user.Token);

        var notVinted = await UpdateProfileAsync("https://example.com/member/12345");
        Assert.Equal(HttpStatusCode.BadRequest, notVinted.StatusCode);

        var notHttps = await UpdateProfileAsync("http://www.vinted.co.uk/member/12345");
        Assert.Equal(HttpStatusCode.BadRequest, notHttps.StatusCode);
    }

    [Fact]
    public async Task UpdatingProfile_WithLookalikeVintedUrl_ReturnsBadRequest()
    {
        var user = await _client.RegisterAsync("alice");
        _client.Authenticate(user.Token);

        var subdomainTrick = await UpdateProfileAsync("https://vinted.evil.com/member/12345");
        Assert.Equal(HttpStatusCode.BadRequest, subdomainTrick.StatusCode);

        var prefixTrick = await UpdateProfileAsync("https://www.vinted-shop.com/member/12345");
        Assert.Equal(HttpStatusCode.BadRequest, prefixTrick.StatusCode);
    }

    private record MessageResult(string Message);

    private Task<HttpResponseMessage> ChangeUsernameAsync(string newUserName) =>
        _client.PutAsJsonAsync(
            "/api/auth/profile",
            new
            {
                userName = newUserName,
                displayName = "Alice",
                bio = "",
                vintedUrl = "",
            }
        );

    [Fact]
    public async Task ChangingUsername_ToAValidNewName_Works()
    {
        var user = await _client.RegisterAsync("alice");
        _client.Authenticate(user.Token);

        var response = await ChangeUsernameAsync("alice.reads");

        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<ProfileResult>();
        Assert.Equal("alice.reads", profile!.UserName);
    }

    [Fact]
    public async Task ChangingUsername_TwiceWithin30Days_IsBlocked()
    {
        var user = await _client.RegisterAsync("alice");
        _client.Authenticate(user.Token);

        (await ChangeUsernameAsync("alice.reads")).EnsureSuccessStatusCode();

        var second = await ChangeUsernameAsync("alice.books");
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Contains("once every 30 days", body!.Message);
    }

    [Fact]
    public async Task ChangingUsername_ToATakenName_IsBlocked()
    {
        var otherClient = _factory.CreateClient();
        await otherClient.RegisterAsync("bobby");

        var user = await _client.RegisterAsync("alice");
        _client.Authenticate(user.Token);

        var response = await ChangeUsernameAsync("bobby");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangingUsername_ToTooShort_IsBlocked()
    {
        var user = await _client.RegisterAsync("alice");
        _client.Authenticate(user.Token);

        var response = await ChangeUsernameAsync("abc");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}