using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class MessagesTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public MessagesTests()
    {
        _factory = new TestWebAppFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private record MessageResult(int Id, string SenderId, string SenderName, string Text);

    private record ConversationResult(
        string BookTitle,
        string OtherUserName,
        MessageResult[] Messages
    );

    private record MessageErrorResult(string Message);

    private async Task<(
        HttpClient OwnerClient,
        HttpClient BorrowerClient,
        BorrowResult Request
    )> SetUpRequestAsync()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var book = await ownerClient.AddBookAsync("Shared Book", offer: "available-to-borrow");

        var borrowerClient = _factory.CreateClient();
        var borrower = await borrowerClient.RegisterAsync("borrower");
        borrowerClient.Authenticate(borrower.Token);
        var request = await borrowerClient.RequestBookAsync(book.Id, "May I borrow this?");

        return (ownerClient, borrowerClient, request);
    }

    [Fact]
    public async Task BothParties_CanChatAndSeeTheThreadInOrder()
    {
        var (ownerClient, borrowerClient, request) = await SetUpRequestAsync();

        var first = await borrowerClient.PostAsJsonAsync(
            "/api/messages",
            new { borrowRequestId = request.Id, text = "Hi! Is Saturday ok for pickup?" }
        );
        first.EnsureSuccessStatusCode();

        var second = await ownerClient.PostAsJsonAsync(
            "/api/messages",
            new { borrowRequestId = request.Id, text = "Saturday works, 2pm at the library?" }
        );
        second.EnsureSuccessStatusCode();

        var conversation = await borrowerClient.GetFromJsonAsync<ConversationResult>(
            $"/api/messages/request/{request.Id}"
        );

        Assert.Equal("Shared Book", conversation!.BookTitle);
        Assert.Equal("owner", conversation.OtherUserName);
        Assert.Equal(2, conversation.Messages.Length);
        Assert.Equal("Hi! Is Saturday ok for pickup?", conversation.Messages[0].Text);
        Assert.Equal("borrower", conversation.Messages[0].SenderName);
        Assert.Equal("Saturday works, 2pm at the library?", conversation.Messages[1].Text);
        Assert.Equal("owner", conversation.Messages[1].SenderName);
    }

    [Fact]
    public async Task SomeoneElse_CannotSendOrReadTheConversation()
    {
        var (_, borrowerClient, request) = await SetUpRequestAsync();

        await borrowerClient.PostAsJsonAsync(
            "/api/messages",
            new { borrowRequestId = request.Id, text = "Private plans" }
        );

        var intruderClient = _factory.CreateClient();
        var intruder = await intruderClient.RegisterAsync("intruder");
        intruderClient.Authenticate(intruder.Token);

        var send = await intruderClient.PostAsJsonAsync(
            "/api/messages",
            new { borrowRequestId = request.Id, text = "Let me in" }
        );
        Assert.Equal(HttpStatusCode.Forbidden, send.StatusCode);

        var read = await intruderClient.GetAsync($"/api/messages/request/{request.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
    }

    [Fact]
    public async Task SendingAnEmptyMessage_ReturnsBadRequest()
    {
        var (_, borrowerClient, request) = await SetUpRequestAsync();

        var response = await borrowerClient.PostAsJsonAsync(
            "/api/messages",
            new { borrowRequestId = request.Id, text = "   " }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageErrorResult>();
        Assert.Equal("Message can't be empty", body!.Message);
    }

    private record UnreadResult(int Count);

    [Fact]
    public async Task UnreadCount_TracksNewMessagesAndClearsWhenConversationOpened()
    {
        var (ownerClient, borrowerClient, request) = await SetUpRequestAsync();

        await borrowerClient.PostAsJsonAsync(
            "/api/messages",
            new { borrowRequestId = request.Id, text = "Hello!" }
        );

        var ownerUnread = await ownerClient.GetFromJsonAsync<UnreadResult>(
            "/api/messages/unread-count"
        );
        Assert.Equal(1, ownerUnread!.Count);

        var borrowerUnread = await borrowerClient.GetFromJsonAsync<UnreadResult>(
            "/api/messages/unread-count"
        );
        Assert.Equal(0, borrowerUnread!.Count);

        await ownerClient.GetAsync($"/api/messages/request/{request.Id}");

        ownerUnread = await ownerClient.GetFromJsonAsync<UnreadResult>(
            "/api/messages/unread-count"
        );
        Assert.Equal(0, ownerUnread!.Count);
    }

    [Fact]
    public async Task MessagingOnANonexistentRequest_ReturnsNotFound()
    {
        var (_, borrowerClient, _) = await SetUpRequestAsync();

        var response = await borrowerClient.PostAsJsonAsync(
            "/api/messages",
            new { borrowRequestId = 9999, text = "Hello?" }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
