using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;
using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController : ControllerBase
{
    public const int MostBooks = 1000;

    private readonly AppDbContext _context;
    private readonly ReadingHistory _readings;

    public GoalsController(AppDbContext context, ReadingHistory readings)
    {
        _context = context;
        _readings = readings;
    }

    [HttpGet]
    public async Task<IActionResult> GetGoals()
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var targets = await _context
            .ReadingGoals.Where(g => g.UserId == me)
            .ToDictionaryAsync(g => g.Year, g => g.Target);

        var read = await _readings.BooksReadByYearAsync(me);

        var years = targets
            .Keys.Union(read.Keys)
            .Append(DateTime.UtcNow.Year)
            .Distinct()
            .OrderByDescending(y => y);

        return Ok(
            years.Select(year => new GoalResponse
            {
                Year = year,
                Target = targets.TryGetValue(year, out var target) ? target : null,
                BooksRead = read.GetValueOrDefault(year),
            })
        );
    }

    [HttpPut("{year}")]
    public async Task<IActionResult> SetGoal(int year, [FromBody] SetGoalRequest request)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (year < 1900 || year > DateTime.UtcNow.Year + 1)
        {
            return BadRequest(new { message = "Pick a year from 1900 to next year." });
        }

        if (request.Target is < 1 or > MostBooks)
        {
            return BadRequest(
                new { message = $"Your goal needs to be between 1 and {MostBooks} books." }
            );
        }

        var goal = await _context.ReadingGoals.FirstOrDefaultAsync(g =>
            g.UserId == me && g.Year == year
        );

        if (goal == null)
        {
            goal = new ReadingGoal { UserId = me, Year = year };
            _context.ReadingGoals.Add(goal);
        }

        goal.Target = request.Target;
        await _context.SaveChangesAsync();

        return Ok(
            new GoalResponse
            {
                Year = year,
                Target = goal.Target,
                BooksRead = await _readings.BooksReadInAsync(me, year),
            }
        );
    }

    [HttpDelete("{year}")]
    public async Task<IActionResult> RemoveGoal(int year)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var goal = await _context.ReadingGoals.FirstOrDefaultAsync(g =>
            g.UserId == me && g.Year == year
        );

        if (goal != null)
        {
            _context.ReadingGoals.Remove(goal);
            await _context.SaveChangesAsync();
        }

        return Ok(
            new GoalResponse
            {
                Year = year,
                Target = null,
                BooksRead = await _readings.BooksReadInAsync(me, year),
            }
        );
    }
}

public class SetGoalRequest
{
    public int Target { get; set; }
}

public class GoalResponse
{
    public int Year { get; set; }
    public int? Target { get; set; }
    public int BooksRead { get; set; }
}
