using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public class ReadingHistory
{
    public const string FutureMessage = "You can't finish a book in the future.";
    public const string TooEarlyMessage = "That date is too far back.";

    private static readonly DateTime Earliest = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan FurthestAhead = TimeSpan.FromHours(14);
    private static readonly TimeSpan FurthestBehind = TimeSpan.FromHours(12);

    private readonly AppDbContext _context;

    public ReadingHistory(AppDbContext context)
    {
        _context = context;
    }

    public static DateTime DayOf(DateTime when) => DateTime.SpecifyKind(when.Date, DateTimeKind.Utc);

    public static string? ProblemWith(DateTime finishedDate) =>
        DayOf(finishedDate) < Earliest ? TooEarlyMessage
        : DayOf(finishedDate) > DayOf(DateTime.UtcNow + FurthestAhead) ? FutureMessage
        : null;

    public static DateTime TodayFor(DateTime? localToday)
    {
        var now = DateTime.UtcNow;

        return
            localToday is { } day
            && DayOf(day) >= DayOf(now - FurthestBehind)
            && DayOf(day) <= DayOf(now + FurthestAhead)
            ? DayOf(day)
            : DayOf(now);
    }

    public async Task<ReadingSession> RecordAsync(int libraryEntryId, DateTime finishedDate)
    {
        var day = DayOf(finishedDate);

        var alreadyLogged = await _context.ReadingSessions.FirstOrDefaultAsync(r =>
            r.LibraryEntryId == libraryEntryId && r.FinishedDate == day
        );

        if (alreadyLogged != null)
        {
            return alreadyLogged;
        }

        var finish = new ReadingSession { LibraryEntryId = libraryEntryId, FinishedDate = day };
        _context.ReadingSessions.Add(finish);

        return finish;
    }
}
