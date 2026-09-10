using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record NotificationResult(
    int Id,
    string Type,
    bool IsRead,
    string ActorUserName,
    string ActorDisplayName
);

public record UnreadCountResult(int Count);

public class NotificationTests : IDisposable
{
    private readonly TestWebAppFactory _factory;

    public NotificationTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> SignInAsync(string name)
    {
        var client = _factory.CreateClient();
        var user = await client.RegisterAsync(name);
        client.Authenticate(user.Token);
        return client;
    }

    private static Task GoPrivateAsync(HttpClient client, string username) =>
        client.PutAsJsonAsync(
            "/api/auth/profile",
            new
            {
                userName = username,
                displayName = username,
                bio = "",
                vintedUrl = "",
                isPrivate = true,
            }
        );

    private static async Task<NotificationResult[]> NotificationsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<NotificationResult[]>("/api/notifications"))!;

    private static async Task<int> UnreadCountAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<UnreadCountResult>("/api/notifications/unread-count"))!.Count;

    [Fact]
    public async Task FollowingAPublicAccountNotifiesThem()
    {
        var rebel = await SignInAsync("rebel");
        var beth = await SignInAsync("beth");

        await beth.PostAsync("/api/users/rebel/follow", null);

        var theirs = await NotificationsAsync(rebel);

        Assert.Equal("new-follower", theirs.Single().Type);
        Assert.Equal("beth", theirs.Single().ActorUserName);
        Assert.Equal(1, await UnreadCountAsync(rebel));
    }

    [Fact]
    public async Task RequestingAPrivateAccountNotifiesThem()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");
        await beth.PostAsync("/api/users/rebel/follow", null);

        Assert.Equal("follow-requested", (await NotificationsAsync(rebel)).Single().Type);
    }

    [Fact]
    public async Task ApprovingAFollowNotifiesTheRequester()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");
        await beth.PostAsync("/api/users/rebel/follow", null);
        await rebel.PostAsync("/api/users/beth/approve-follow", null);

        var mine = await NotificationsAsync(beth);

        Assert.Equal("follow-approved", mine.Single().Type);
        Assert.Equal("rebel", mine.Single().ActorUserName);
    }

    [Fact]
    public async Task DecliningNotifiesNobody()
    {
        var rebel = await SignInAsync("rebel");
        await GoPrivateAsync(rebel, "rebel");

        var beth = await SignInAsync("beth");
        await beth.PostAsync("/api/users/rebel/follow", null);
        await rebel.PostAsync("/api/users/beth/decline-follow", null);

        Assert.Empty(await NotificationsAsync(beth));
    }

    [Fact]
    public async Task UnfollowingClearsTheNotificationItCreated()
    {
        var rebel = await SignInAsync("rebel");
        var beth = await SignInAsync("beth");

        await beth.PostAsync("/api/users/rebel/follow", null);
        await beth.DeleteAsync("/api/users/rebel/follow");

        Assert.Empty(await NotificationsAsync(rebel));
    }

    [Fact]
    public async Task MarkingAsReadClearsTheUnreadCount()
    {
        var rebel = await SignInAsync("rebel");
        var beth = await SignInAsync("beth");

        await beth.PostAsync("/api/users/rebel/follow", null);
        (await rebel.PostAsync("/api/notifications/read", null)).EnsureSuccessStatusCode();

        Assert.Equal(0, await UnreadCountAsync(rebel));
        Assert.True((await NotificationsAsync(rebel)).Single().IsRead);
    }

    [Fact]
    public async Task ClearingRemovesEverything()
    {
        var rebel = await SignInAsync("rebel");
        var beth = await SignInAsync("beth");

        await beth.PostAsync("/api/users/rebel/follow", null);
        (await rebel.DeleteAsync("/api/notifications")).EnsureSuccessStatusCode();

        Assert.Empty(await NotificationsAsync(rebel));
        Assert.Equal(0, await UnreadCountAsync(rebel));
    }

    [Fact]
    public async Task YouOnlySeeYourOwnNotifications()
    {
        var rebel = await SignInAsync("rebel");
        var beth = await SignInAsync("beth");

        await beth.PostAsync("/api/users/rebel/follow", null);

        Assert.Empty(await NotificationsAsync(beth));
    }
}
