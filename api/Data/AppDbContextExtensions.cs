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

    public static int FillMissingAuthorSorts(this AppDbContext context)
    {
        var books = context.Books.Where(b => b.AuthorSort == "" && b.Author != "").ToList();

        foreach (var book in books)
        {
            book.RefreshAuthorSort();
        }

        context.SaveChanges();
        return books.Count;
    }
}
