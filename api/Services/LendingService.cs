using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public class LendingService
{
    public const int Max = 3;

    public const string FullMessage =
        "You already have 3 books offered to borrow. Take one back before offering another.";

    public const string OnLoanMessage = "This book is out on loan. Mark it returned first.";

    public const string NotOwnedMessage = "You can only offer books you own.";

    public static bool CanOffer(string format) => !BookFormat.CannotOffer.Contains(format);

    private readonly AppDbContext _context;

    public LendingService(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> InPoolAsync(string userId) =>
        _context.LibraryEntries.CountAsync(e =>
            e.UserId == userId && BookOffer.Lendable.Contains(e.Offer)
        );

    public async Task<bool> HasRoomAsync(string userId) => await InPoolAsync(userId) < Max;

    public Task<bool> OnLoanAsync(int libraryEntryId) =>
        _context.BorrowRequests.AnyAsync(r =>
            r.LibraryEntryId == libraryEntryId && r.Status == BorrowStatus.Accepted
        );

    public async Task<List<BorrowRequest>> DeclinePendingAsync(int libraryEntryId)
    {
        var pending = await _context
            .BorrowRequests.Where(r =>
                r.LibraryEntryId == libraryEntryId && r.Status == BorrowStatus.Pending
            )
            .ToListAsync();

        foreach (var request in pending)
        {
            request.Status = BorrowStatus.Declined;
        }

        return pending;
    }
}
