using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public class ReaderAccess
{
    private readonly AppDbContext _context;

    public ReaderAccess(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanViewLibraryAsync(AppUser them, string? me)
    {
        if (!them.IsPrivate || them.Id == me)
        {
            return true;
        }

        return await _context.Follows.AnyAsync(f =>
            f.FollowerId == me && f.FollowingId == them.Id && f.Approved
        );
    }

    public async Task<string> FollowStateAsync(string? me, string themId)
    {
        if (me == null)
        {
            return FollowStates.None;
        }

        if (me == themId)
        {
            return FollowStates.Self;
        }

        var follow = await _context.Follows.FirstOrDefaultAsync(f =>
            f.FollowerId == me && f.FollowingId == themId
        );

        if (follow == null)
        {
            return FollowStates.None;
        }

        return follow.Approved ? FollowStates.Following : FollowStates.Requested;
    }
}
