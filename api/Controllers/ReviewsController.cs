using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public ReviewsController(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("book/{bookId}")]
    public async Task<IActionResult> GetReviewsForBook(int bookId)
    {
        var reviews = await _context
            .Reviews.Where(r => r.BookId == bookId)
            .Select(r => new ReviewResponse
            {
                Id = r.Id,
                Rating = r.Rating,
                Text = r.Text,
                ContainsSpoiler = r.ContainsSpoiler,
                Date = r.Date,
                BookId = r.BookId,
                UserId = r.UserId,
                UserName = r.User.DisplayName,
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddReview([FromBody] AddReviewRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var problem = ValidateReview(request.Rating, request.Text);

        if (problem != null)
        {
            return BadRequest(new { message = problem });
        }

        var book = await _context.Books.FindAsync(request.BookId);

        if (book == null)
        {
            return NotFound(new { message = "Book not found" });
        }

        var alreadyReviewed = await _context.Reviews.AnyAsync(r =>
            r.BookId == request.BookId && r.UserId == userId
        );

        if (alreadyReviewed)
        {
            return BadRequest(new { message = "You have already reviewed this book" });
        }

        var review = new Review
        {
            Rating = request.Rating,
            Text = request.Text,
            ContainsSpoiler = request.ContainsSpoiler,
            BookId = request.BookId,
            UserId = userId!,
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId!);

        return Ok(
            new ReviewResponse
            {
                Id = review.Id,
                Rating = review.Rating,
                Text = review.Text,
                ContainsSpoiler = review.ContainsSpoiler,
                Date = review.Date,
                BookId = review.BookId,
                UserId = review.UserId,
                UserName = user?.DisplayName ?? "Unknown",
            }
        );
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var problem = ValidateReview(request.Rating, request.Text);

        if (problem != null)
        {
            return BadRequest(new { message = problem });
        }

        var review = await _context.Reviews.FindAsync(id);

        if (review == null || review.UserId != userId)
        {
            return NotFound(new { message = "Review not found" });
        }

        review.Rating = request.Rating;
        review.Text = request.Text;
        review.ContainsSpoiler = request.ContainsSpoiler;

        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId!);

        return Ok(
            new ReviewResponse
            {
                Id = review.Id,
                Rating = review.Rating,
                Text = review.Text,
                ContainsSpoiler = review.ContainsSpoiler,
                Date = review.Date,
                BookId = review.BookId,
                UserId = review.UserId,
                UserName = user?.DisplayName ?? "Unknown",
            }
        );
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var review = await _context.Reviews.FindAsync(id);

        if (review == null || review.UserId != userId)
        {
            return NotFound(new { message = "Review not found" });
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return Ok();
    }

    private static string? ValidateReview(int? rating, string text)
    {
        if (rating is < 1 or > 5)
        {
            return "Rating must be between 1 and 5";
        }

        if (rating == null && string.IsNullOrWhiteSpace(text))
        {
            return "Add a rating or write a review";
        }

        return null;
    }
}

public class AddReviewRequest
{
    public int? Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool ContainsSpoiler { get; set; }
    public int BookId { get; set; }
}

public class UpdateReviewRequest
{
    public int? Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool ContainsSpoiler { get; set; }
}

public class ReviewResponse
{
    public int Id { get; set; }
    public int? Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool ContainsSpoiler { get; set; }
    public DateTime Date { get; set; }
    public int BookId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
