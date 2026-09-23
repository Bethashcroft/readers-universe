using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public record GoalResult(int Year, int? Target, int BooksRead);

public record ProfileGoalResult(GoalResult? Goal);

public class GoalTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private static readonly int ThisYear = DateTime.UtcNow.Year;

    public GoalTests() => _factory = new TestWebAppFactory();

    public void Dispose() => _factory.Dispose();

    private static async Task<GoalResult[]> GoalsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<GoalResult[]>("/api/goals"))!;

    private static Task<HttpResponseMessage> SetGoalAsync(
        HttpClient client,
        int year,
        int target
    ) => client.PutAsJsonAsync($"/api/goals/{year}", new { target });

    private static async Task<BookResult> FinishAsync(HttpClient client, string title)
    {
        var book = await client.AddBookAsync(title, shelf: "currently-reading");
        await client.PutAsJsonAsync(
            $"/api/library/{book.Id}",
            new { shelf = "read", offer = "none" }
        );
        return book;
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

    [Fact]
    public async Task ThisYearIsAlwaysOnTheList()
    {
        var sophie = await _factory.SignInAsync("sophie");

        var goals = await GoalsAsync(sophie);

        Assert.Equal(new GoalResult(ThisYear, null, 0), Assert.Single(goals));
    }

    [Fact]
    public async Task YourGoalCountsTheBooksYouFinishedThisYear()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await FinishAsync(sophie, "Babel");
        await FinishAsync(sophie, "Piranesi");

        var response = await SetGoalAsync(sophie, ThisYear, 50);
        response.EnsureSuccessStatusCode();

        var goal = (await response.Content.ReadFromJsonAsync<GoalResult>())!;
        Assert.Equal(new GoalResult(ThisYear, 50, 2), goal);
    }

    [Fact]
    public async Task PastYearsShowWhatYouReadEvenWithoutAGoal()
    {
        var sophie = await _factory.SignInAsync("sophie");
        var book = await FinishAsync(sophie, "Babel");
        var finish = (
            await sophie.GetFromJsonAsync<ReadingResult[]>($"/api/library/{book.Id}/readings")
        )!.Single();
        await sophie.PutAsJsonAsync(
            $"/api/library/readings/{finish.Id}",
            new { finishedDate = new DateTime(ThisYear - 1, 6, 1) }
        );

        var goals = await GoalsAsync(sophie);

        Assert.Equal(
            [new GoalResult(ThisYear, null, 0), new GoalResult(ThisYear - 1, null, 1)],
            goals
        );
    }

    [Fact]
    public async Task ChangingYourGoalReplacesIt()
    {
        var sophie = await _factory.SignInAsync("sophie");

        await SetGoalAsync(sophie, ThisYear, 10);
        await SetGoalAsync(sophie, ThisYear, 20);

        Assert.Equal(20, Assert.Single(await GoalsAsync(sophie)).Target);
    }

    [Fact]
    public async Task RemovingYourGoalKeepsTheCount()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await FinishAsync(sophie, "Babel");
        await SetGoalAsync(sophie, ThisYear, 10);

        (await sophie.DeleteAsync($"/api/goals/{ThisYear}")).EnsureSuccessStatusCode();

        Assert.Equal(new GoalResult(ThisYear, null, 1), Assert.Single(await GoalsAsync(sophie)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task AGoalHasToBeSensible(int target)
    {
        var sophie = await _factory.SignInAsync("sophie");

        var response = await SetGoalAsync(sophie, ThisYear, target);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task YouCannotSetAGoalForTheDistantFuture()
    {
        var sophie = await _factory.SignInAsync("sophie");

        var response = await SetGoalAsync(sophie, ThisYear + 5, 10);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task YourGoalIsYours()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await SetGoalAsync(sophie, ThisYear, 50);

        var tom = await _factory.SignInAsync("tom");

        Assert.Null(Assert.Single(await GoalsAsync(tom)).Target);
    }

    [Fact]
    public async Task OtherReadersSeeYourGoalOnYourProfile()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await FinishAsync(sophie, "Babel");
        await SetGoalAsync(sophie, ThisYear, 50);

        var tom = await _factory.SignInAsync("tom");
        var profile = await tom.GetFromJsonAsync<ProfileGoalResult>("/api/users/sophie");

        Assert.Equal(new GoalResult(ThisYear, 50, 1), profile!.Goal);
    }

    [Fact]
    public async Task APrivateAccountKeepsItsGoalFromStrangers()
    {
        var sophie = await _factory.SignInAsync("sophie");
        await GoPrivateAsync(sophie, "sophie");
        await SetGoalAsync(sophie, ThisYear, 50);

        var tom = await _factory.SignInAsync("tom");
        var profile = await tom.GetFromJsonAsync<ProfileGoalResult>("/api/users/sophie");

        Assert.Null(profile!.Goal);
    }
}
