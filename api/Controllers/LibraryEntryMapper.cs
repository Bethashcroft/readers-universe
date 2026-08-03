using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

public static class LibraryEntryMapper
{
    public static async Task<List<LibraryEntryResponse>> MapAsync(
        AppDbContext context,
        List<LibraryEntry> entries,
        Func<LibraryEntry, string> ratingUserSelector
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
                Rating = lookup.GetValueOrDefault((e.BookId, ratingUserSelector(e))),
                UserId = e.UserId,
                OwnerName = e.User.DisplayName,
                OwnerUserName = e.User.UserName ?? string.Empty,
                SellerVintedUrl = e.User.VintedUrl,
            })
            .ToList();
    }
}
