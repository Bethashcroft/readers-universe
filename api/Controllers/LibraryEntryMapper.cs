using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

public static class LibraryEntryMapper
{
    public static async Task<List<LibraryEntryResponse>> MapAsync(
        AppDbContext context,
        List<LibraryEntry> entries,
        Func<LibraryEntry, string> ratingUserSelector,
        string? viewerId
    )
    {
        if (entries.Count == 0)
        {
            return [];
        }

        var bookIds = entries.Select(e => e.BookId).Distinct().ToList();
        var userIds = entries.Select(ratingUserSelector).Distinct().ToList();

        var ratings = await context
            .Reviews.Where(r => bookIds.Contains(r.BookId) && userIds.Contains(r.UserId))
            .Select(r => new
            {
                r.BookId,
                r.UserId,
                r.Rating,
            })
            .ToListAsync();

        var lookup = ratings.ToDictionary(r => (r.BookId, r.UserId), r => r.Rating);

        var myEntryIds = entries.Where(e => e.UserId == viewerId).Select(e => e.Id).ToList();

        var readings =
            myEntryIds.Count == 0
                ? []
                : (
                    await context
                        .ReadingSessions.Where(r => myEntryIds.Contains(r.LibraryEntryId))
                        .GroupBy(r => r.LibraryEntryId)
                        .Select(g => new
                        {
                            LibraryEntryId = g.Key,
                            Finished = g.Max(r => r.FinishedDate),
                            Times = g.Count(),
                        })
                        .ToListAsync()
                ).ToDictionary(
                    r => r.LibraryEntryId,
                    r => new ReadingSummary(r.Finished, r.Times)
                );

        return entries
            .Select(e => new LibraryEntryResponse
            {
                Id = e.Id,
                BookId = e.BookId,
                Title = e.Book.Title,
                Author = e.Book.Author,
                CoverUrl = e.Book.CoverUrl,
                Isbn = e.Book.Isbn,
                Shelf = e.Shelf,
                Offer = e.Offer,
                Format = e.Format,
                Page = e.Page,
                PageCount = e.PageCount,
                FinishedDate = readings.GetValueOrDefault(e.Id)?.Finished,
                TimesRead = readings.GetValueOrDefault(e.Id)?.Times ?? 0,
                Rating = lookup.GetValueOrDefault((e.BookId, ratingUserSelector(e))),
                UserId = e.UserId,
                OwnerName = e.User.DisplayName,
                OwnerUserName = e.User.UserName ?? string.Empty,
                SellerVintedUrl = e.User.VintedUrl,
            })
            .ToList();
    }

    private record ReadingSummary(DateTime Finished, int Times);
}
