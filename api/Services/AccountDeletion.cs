using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public class AccountDeletion
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public AccountDeletion(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<bool> DeleteAsync(AppUser user)
    {
        var id = user.Id;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var booksTheyHaveBorrowed = _context
            .BorrowRequests.Where(r => r.FromUserId == id && r.Status == BorrowStatus.Accepted)
            .Select(r => r.LibraryEntryId);

        await _context
            .LibraryEntries.Where(e =>
                booksTheyHaveBorrowed.Contains(e.Id) && e.Offer == BookOffer.LentOut
            )
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Offer, BookOffer.None));

        await _context
            .BorrowRequests.Where(r => r.FromUserId == id || r.ToUserId == id)
            .ExecuteDeleteAsync();

        await _context.Likes.Where(l => l.UserId == id).ExecuteDeleteAsync();
        await _context.Comments.Where(c => c.UserId == id).ExecuteDeleteAsync();

        await _context
            .Notifications.Where(n => n.UserId == id || n.ActorId == id)
            .ExecuteDeleteAsync();

        await _context
            .Activities.Where(a => a.UserId == id || a.TargetUserId == id)
            .ExecuteDeleteAsync();

        await _context
            .Follows.Where(f => f.FollowerId == id || f.FollowingId == id)
            .ExecuteDeleteAsync();

        await _context
            .Trusts.Where(t => t.TrusterId == id || t.TrustedId == id)
            .ExecuteDeleteAsync();

        await _context.Reviews.Where(r => r.UserId == id).ExecuteDeleteAsync();
        await _context.LibraryEntries.Where(e => e.UserId == id).ExecuteDeleteAsync();

        var deleted = await _userManager.DeleteAsync(user);

        if (!deleted.Succeeded)
        {
            return false;
        }

        await transaction.CommitAsync();
        return true;
    }
}
