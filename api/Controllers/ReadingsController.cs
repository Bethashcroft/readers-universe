using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;
using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/library")]
[Authorize]
public class ReadingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReadingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}/readings")]
    public async Task<IActionResult> GetReadings(int id)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!await IsMineAsync(id, me))
        {
            return NotFound(new { message = "Book not found on your shelves" });
        }

        return Ok(await ReadingsForAsync(id));
    }

    [HttpPut("readings/{readingId}")]
    public async Task<IActionResult> EditReading(
        int readingId,
        [FromBody] ReadingRequest request
    )
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var reading = await MineAsync(readingId, me);

        if (reading == null)
        {
            return NotFound(new { message = "That reading has gone." });
        }

        var problem = ReadingHistory.ProblemWith(request.FinishedDate);

        if (problem != null)
        {
            return BadRequest(new { message = problem });
        }

        var day = ReadingHistory.DayOf(request.FinishedDate);

        var clashes = await _context.ReadingSessions.AnyAsync(r =>
            r.LibraryEntryId == reading.LibraryEntryId
            && r.FinishedDate == day
            && r.Id != readingId
        );

        if (clashes)
        {
            return BadRequest(new { message = "That day is already on your list." });
        }

        reading.FinishedDate = day;
        reading.LibraryEntry.ReadingsEdited = true;
        await _context.SaveChangesAsync();

        return Ok(await ReadingsForAsync(reading.LibraryEntryId));
    }

    [HttpDelete("readings/{readingId}")]
    public async Task<IActionResult> DeleteReading(int readingId, [FromQuery] bool keepPost)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var reading = await MineAsync(readingId, me);

        if (reading == null)
        {
            return NotFound(new { message = "That reading has gone." });
        }

        var entryId = reading.LibraryEntryId;

        if (!keepPost)
        {
            var posts = await _context
                .Activities.Where(a => a.ReadingSessionId == readingId)
                .ToListAsync();

            _context.Activities.RemoveRange(posts);
        }

        _context.ReadingSessions.Remove(reading);
        reading.LibraryEntry.ReadingsEdited = true;
        await _context.SaveChangesAsync();

        return Ok(await ReadingsForAsync(entryId));
    }

    private Task<bool> IsMineAsync(int libraryEntryId, string? me) =>
        _context.LibraryEntries.AnyAsync(e => e.Id == libraryEntryId && e.UserId == me);

    private Task<ReadingSession?> MineAsync(int readingId, string? me) =>
        _context
            .ReadingSessions.Include(r => r.LibraryEntry)
            .Where(r => r.Id == readingId && r.LibraryEntry.UserId == me)
            .FirstOrDefaultAsync();

    private Task<List<ReadingResponse>> ReadingsForAsync(int libraryEntryId) =>
        _context
            .ReadingSessions.Where(r => r.LibraryEntryId == libraryEntryId)
            .OrderByDescending(r => r.FinishedDate)
            .ThenByDescending(r => r.Id)
            .Select(r => new ReadingResponse
            {
                Id = r.Id,
                FinishedDate = r.FinishedDate,
                HasPost = _context.Activities.Any(a => a.ReadingSessionId == r.Id),
                LikeCount = _context
                    .Activities.Where(a => a.ReadingSessionId == r.Id)
                    .Sum(a => a.Likes.Count),
                CommentCount = _context
                    .Activities.Where(a => a.ReadingSessionId == r.Id)
                    .Sum(a => a.Comments.Count),
            })
            .ToListAsync();
}

public class ReadingRequest
{
    public DateTime FinishedDate { get; set; }
}

public class ReadingResponse
{
    public int Id { get; set; }
    public DateTime FinishedDate { get; set; }
    public bool HasPost { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
}
