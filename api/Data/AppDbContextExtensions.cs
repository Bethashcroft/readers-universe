using Microsoft.EntityFrameworkCore;

namespace ReadersRealm.Api.Data;

public static class AppDbContextExtensions
{
    public static async Task<bool> TrySaveChangesAsync(this AppDbContext context)
    {
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            return false;
        }
    }
}
