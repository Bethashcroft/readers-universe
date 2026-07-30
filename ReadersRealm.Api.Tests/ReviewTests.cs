using System.Net;
using System.Net.Http.Json;

namespace ReadersRealm.Api.Tests;

public class ReviewTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public ReviewTests()
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
    public async Task ReviewsPoolAcrossEveryoneWhoOwnsTheBook()
    {
        var bethClient = _factory.CreateClient();
        var beth = await bethClient.RegisterAsync("beth");
        bethClient.Authenticate(beth.Token);
        var hers = await bethClient.AddBookAsync("The Hobbit", author: "J.R.R. Tolkien");
        await bethClient.AddReviewAsync(hers.BookId, 5, "A comfort read");

        var samClient = _factory.CreateClient();
        var sam = await samClient.RegisterAsync("sam");
        samClient.Authenticate(sam.Token);
        var his = await samClient.AddBookAsync("The Hobbit", author: "J.R.R. Tolkien");
        await samClient.AddReviewAsync(his.BookId, 3, "Slow in the middle");

        var reviews = await samClient.GetFromJsonAsync<ReviewResult[]>(
            $"/api/reviews/book/{his.BookId}"
        );

        Assert.Equal(2, reviews!.Length);
        Assert.Contains(reviews, r => r.UserName == "beth" && r.Text == "A comfort read");
        Assert.Contains(reviews, r => r.UserName == "sam");

        var detail = await samClient.GetFromJsonAsync<BookDetailResult>(
            $"/api/books/{his.BookId}"
        );
        Assert.Equal(4, detail!.AverageRating);
        Assert.Equal(2, detail.RatingCount);
    }

    [Fact]
    public async Task ReviewingYourOwnBook_IsAllowed()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        var entry = await _client.AddBookAsync("My Memoir");

        var response = await _client.PostAsJsonAsync(
            "/api/reviews",
            new
            {
                rating = 5,
                text = "Still my favourite",
                bookId = entry.BookId,
            }
        );

        response.EnsureSuccessStatusCode();

        var mine = await _client.GetFromJsonAsync<BookResult[]>("/api/library");
        Assert.Equal(5, mine!.Single().Rating);
    }

    [Fact]
    public async Task AStarOnlyRating_CountsAsAReview()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        var entry = await _client.AddBookAsync("Quietly Rated");

        await _client.AddReviewAsync(entry.BookId, 4, "");

        var reviews = await _client.GetFromJsonAsync<ReviewResult[]>(
            $"/api/reviews/book/{entry.BookId}"
        );

        Assert.Single(reviews!);
        Assert.Equal(string.Empty, reviews!.Single().Text);
        Assert.Equal(4, reviews!.Single().Rating);
    }

    [Fact]
    public async Task DeletingYourReview_ClearsYourRating()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        var entry = await _client.AddBookAsync("My Memoir");

        var review = await _client.AddReviewAsync(entry.BookId, 4, "Good stuff");

        (await _client.DeleteAsync($"/api/reviews/{review.Id}")).EnsureSuccessStatusCode();

        var mine = await _client.GetFromJsonAsync<BookResult[]>("/api/library");
        Assert.Null(mine!.Single().Rating);
    }

    [Fact]
    public async Task ReviewWithRatingOutOfRange_ReturnsBadRequest()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        var entry = await _client.AddBookAsync("Good Book");

        var response = await _client.PostAsJsonAsync(
            "/api/reviews",
            new
            {
                rating = 99,
                text = "off the charts",
                bookId = entry.BookId,
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Equal("Rating must be between 1 and 5", body!.Message);
    }

    [Fact]
    public async Task ReviewingTheSameBookTwice_ReturnsBadRequest()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var entry = await ownerClient.AddBookAsync("Re-readable");

        var reader = await _client.RegisterAsync("reader");
        _client.Authenticate(reader.Token);

        await _client.AddReviewAsync(entry.BookId, 4, "Great");

        var second = await _client.PostAsJsonAsync(
            "/api/reviews",
            new
            {
                rating = 2,
                text = "Changed my mind",
                bookId = entry.BookId,
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Equal("You have already reviewed this book", body!.Message);
    }

    [Fact]
    public async Task EditingYourOwnReview_UpdatesIt()
    {
        var owner = await _client.RegisterAsync("owner");
        _client.Authenticate(owner.Token);
        var entry = await _client.AddBookAsync("Rated Then Reviewed", rating: 3);

        var reviews = await _client.GetFromJsonAsync<ReviewResult[]>(
            $"/api/reviews/book/{entry.BookId}"
        );
        var mine = reviews!.Single();

        var response = await _client.PutAsJsonAsync(
            $"/api/reviews/{mine.Id}",
            new
            {
                rating = 5,
                text = "Came back to add my thoughts",
                containsSpoiler = true,
            }
        );
        response.EnsureSuccessStatusCode();

        var after = await _client.GetFromJsonAsync<SpoilerReviewResult[]>(
            $"/api/reviews/book/{entry.BookId}"
        );
        Assert.True(after!.Single().ContainsSpoiler);

        var updated = await _client.GetFromJsonAsync<ReviewResult[]>(
            $"/api/reviews/book/{entry.BookId}"
        );
        Assert.Equal(5, updated!.Single().Rating);
        Assert.Equal("Came back to add my thoughts", updated!.Single().Text);

        var library = await _client.GetFromJsonAsync<BookResult[]>("/api/library");
        Assert.Equal(5, library!.Single().Rating);
    }

    [Fact]
    public async Task EditingSomeoneElsesReview_ReturnsNotFound()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var entry = await ownerClient.AddBookAsync("Reviewed Book");
        var review = await ownerClient.AddReviewAsync(entry.BookId);

        var intruder = await _client.RegisterAsync("intruder");
        _client.Authenticate(intruder.Token);

        var response = await _client.PutAsJsonAsync(
            $"/api/reviews/{review.Id}",
            new { rating = 1, text = "hijacked", containsSpoiler = false }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletingYourOwnReview_RemovesIt()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var entry = await ownerClient.AddBookAsync("Reviewed Book");

        var reader = await _client.RegisterAsync("reader");
        _client.Authenticate(reader.Token);
        var review = await _client.AddReviewAsync(entry.BookId);

        var deleteResponse = await _client.DeleteAsync($"/api/reviews/{review.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var remaining = await _client.GetFromJsonAsync<ReviewResult[]>(
            $"/api/reviews/book/{entry.BookId}"
        );
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task DeletingSomeoneElsesReview_ReturnsNotFound()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var entry = await ownerClient.AddBookAsync("Reviewed Book");

        var reader = await _client.RegisterAsync("reader");
        _client.Authenticate(reader.Token);
        var review = await _client.AddReviewAsync(entry.BookId);

        var intruderClient = _factory.CreateClient();
        var intruder = await intruderClient.RegisterAsync("intruder");
        intruderClient.Authenticate(intruder.Token);

        var deleteResponse = await intruderClient.DeleteAsync($"/api/reviews/{review.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task AddingASpoilerReview_PersistsAndReturnsTheFlag()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("owner");
        ownerClient.Authenticate(owner.Token);
        var entry = await ownerClient.AddBookAsync("Twist Ending");

        var reader = await _client.RegisterAsync("reader");
        _client.Authenticate(reader.Token);

        var response = await _client.PostAsJsonAsync(
            "/api/reviews",
            new
            {
                rating = 5,
                text = "The butler did it!",
                containsSpoiler = true,
                bookId = entry.BookId,
            }
        );
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<SpoilerReviewResult>();
        Assert.True(created!.ContainsSpoiler);

        var reviews = await _client.GetFromJsonAsync<SpoilerReviewResult[]>(
            $"/api/reviews/book/{entry.BookId}"
        );
        Assert.True(reviews!.Single().ContainsSpoiler);
    }

    private record SpoilerReviewResult(int Id, bool ContainsSpoiler);

    private record MessageResult(string Message);
}
