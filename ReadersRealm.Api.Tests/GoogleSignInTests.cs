using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record GoogleSignUpResult(string SuggestedUserName, string DisplayName, string Email);

public record GoogleSignInResult(AuthResult? Auth, GoogleSignUpResult? SignUp);

public class GoogleSignInTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public GoogleSignInTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static readonly string SophiesToken = FakeGoogleTokenValidator.TokenFor(
        "g-sophie",
        "sophie.bell@gmail.com",
        "Sophie Bell"
    );

    private async Task<GoogleSignInResult> SignInAsync(string idToken)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GoogleSignInResult>())!;
    }

    private Task<HttpResponseMessage> RegisterAsync(
        string idToken,
        string userName,
        string displayName = "Sophie Bell"
    ) =>
        _client.PostAsJsonAsync(
            "/api/auth/google/register",
            new
            {
                idToken,
                userName,
                displayName,
            }
        );

    [Fact]
    public async Task SomeoneNewIsAskedToChooseAUsername()
    {
        var result = await SignInAsync(SophiesToken);

        Assert.Null(result.Auth);
        Assert.Equal(
            new GoogleSignUpResult("sophiebell", "Sophie Bell", "sophie.bell@gmail.com"),
            result.SignUp
        );
    }

    [Fact]
    public async Task ChoosingAUsernameCreatesTheAccountAndSignsYouIn()
    {
        var response = await RegisterAsync(SophiesToken, "bookdragon");
        response.EnsureSuccessStatusCode();

        var auth = (await response.Content.ReadFromJsonAsync<AuthResult>())!;
        Assert.Equal("bookdragon", auth.UserName);
        Assert.Equal("Sophie Bell", auth.DisplayName);
        Assert.False(string.IsNullOrEmpty(auth.Token));
    }

    [Fact]
    public async Task NextTimeYouGoStraightIn()
    {
        await RegisterAsync(SophiesToken, "bookdragon");

        var result = await SignInAsync(SophiesToken);

        Assert.Null(result.SignUp);
        Assert.Equal("bookdragon", result.Auth!.UserName);
    }

    [Fact]
    public async Task AnExistingAccountWithTheSameEmailIsLinked()
    {
        var existing = await _client.RegisterAsync("sophie");
        var token = FakeGoogleTokenValidator.TokenFor("g-sophie", "sophie@example.com", "Sophie");

        var result = await SignInAsync(token);

        Assert.Equal(existing.UserId, result.Auth!.UserId);
    }

    [Fact]
    public async Task AnUnverifiedGoogleEmailIsNeverLinkedToSomeoneElsesAccount()
    {
        await _client.RegisterAsync("sophie");
        var token = FakeGoogleTokenValidator.TokenFor(
            "g-impostor",
            "sophie@example.com",
            "Not Sophie",
            verified: false
        );

        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = token });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task TheSuggestionSkipsUsernamesAlreadyTaken()
    {
        await RegisterAsync(SophiesToken, "sophiebell");
        var anotherSophie = FakeGoogleTokenValidator.TokenFor(
            "g-sophie-2",
            "sophie.b@gmail.com",
            "Sophie Bell"
        );

        var result = await SignInAsync(anotherSophie);

        Assert.Equal("sophiebell2", result.SignUp!.SuggestedUserName);
    }

    [Fact]
    public async Task UsernamesFollowTheSameRulesAsEverywhereElse()
    {
        var tooShort = await RegisterAsync(SophiesToken, "sb");
        var spaces = await RegisterAsync(SophiesToken, "sophie bell");

        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, spaces.StatusCode);
        Assert.Equal(
            "Username must be 5 to 20 characters, using only letters, numbers, dots and underscores.",
            await tooShort.ErrorMessageAsync()
        );
    }

    [Fact]
    public async Task ATakenUsernameIsRefused()
    {
        await _client.RegisterAsync("bookdragon");

        var response = await RegisterAsync(SophiesToken, "bookdragon");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("That username is taken.", await response.ErrorMessageAsync());
    }

    [Fact]
    public async Task ATokenGoogleDidNotSignIsRefused()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/google",
            new { idToken = "made-up" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AGoogleAccountCanUseTheRestOfTheApp()
    {
        var response = await RegisterAsync(SophiesToken, "bookdragon");
        var auth = (await response.Content.ReadFromJsonAsync<AuthResult>())!;
        _client.Authenticate(auth.Token);

        var book = await _client.AddBookAsync("Babel");

        Assert.Equal("Babel", book.Title);
    }
}
