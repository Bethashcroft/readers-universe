using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Book> Books { get; set; }
    public DbSet<LibraryEntry> LibraryEntries { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<BorrowRequest> BorrowRequests { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder
            .Entity<AppUser>()
            .HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique()
            .HasFilter("\"NormalizedEmail\" IS NOT NULL");

        builder.Entity<Book>().HasIndex(b => b.MatchKey);
        builder.Entity<Book>().HasIndex(b => b.Isbn);

        builder
            .Entity<LibraryEntry>()
            .HasOne(e => e.Book)
            .WithMany()
            .HasForeignKey(e => e.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<LibraryEntry>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<LibraryEntry>().HasIndex(e => new { e.UserId, e.BookId }).IsUnique();

        builder
            .Entity<BorrowRequest>()
            .HasOne(b => b.LibraryEntry)
            .WithMany()
            .HasForeignKey(b => b.LibraryEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<BorrowRequest>()
            .HasOne(b => b.FromUser)
            .WithMany()
            .HasForeignKey(b => b.FromUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .Entity<BorrowRequest>()
            .HasOne(b => b.ToUser)
            .WithMany()
            .HasForeignKey(b => b.ToUserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder
            .Entity<Review>()
            .HasOne(r => r.Book)
            .WithMany()
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Review>().HasIndex(r => new { r.BookId, r.UserId }).IsUnique();

        builder
            .Entity<Message>()
            .HasOne(m => m.BorrowRequest)
            .WithMany()
            .HasForeignKey(m => m.BorrowRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
