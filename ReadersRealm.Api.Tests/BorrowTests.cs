using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class BorrowTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public BorrowTests()
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
    public async Task RequestingYourOwnBook_ReturnsBadRequest()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);

        var book = await _client.AddBookAsync("My Own Book");

        var response = await _client.PostAsJsonAsync(
            "/api/borrowrequests",
            new {bookId = book.Id, message = "lend me my own book"}
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Equal("You cannot request your own book", body!.Message);
    }

    private record MessageResult(string Message);
}