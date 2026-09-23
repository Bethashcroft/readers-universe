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
    public DbSet<Follow> Follows { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Trust> Trusts { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<ReadingSession> ReadingSessions { get; set; }

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
            .Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany()
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .Entity<Follow>()
            .HasOne(f => f.Following)
            .WithMany()
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Follow>().HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();

        builder
            .Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .Entity<Notification>()
            .HasOne(n => n.Actor)
            .WithMany()
            .HasForeignKey(n => n.ActorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .Entity<Notification>()
            .HasOne(n => n.Activity)
            .WithMany()
            .HasForeignKey(n => n.ActivityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .Entity<Notification>()
            .HasOne(n => n.Book)
            .WithMany()
            .HasForeignKey(n => n.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });

        builder
            .Entity<Trust>()
            .HasOne(t => t.Truster)
            .WithMany()
            .HasForeignKey(t => t.TrusterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .Entity<Trust>()
            .HasOne(t => t.Trusted)
            .WithMany()
            .HasForeignKey(t => t.TrustedId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Trust>().HasIndex(t => new { t.TrusterId, t.TrustedId }).IsUnique();

        builder
            .Entity<Activity>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .Entity<Activity>()
            .HasOne(a => a.Book)
            .WithMany()
            .HasForeignKey(a => a.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<Activity>()
            .HasOne(a => a.TargetUser)
            .WithMany()
            .HasForeignKey(a => a.TargetUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .Entity<Activity>()
            .HasOne(a => a.ReadingSession)
            .WithMany()
            .HasForeignKey(a => a.ReadingSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Activity>().HasIndex(a => new { a.UserId, a.Date });

        builder
            .Entity<Like>()
            .HasOne(l => l.Activity)
            .WithMany(a => a.Likes)
            .HasForeignKey(l => l.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<Like>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Like>().HasIndex(l => new { l.ActivityId, l.UserId }).IsUnique();

        builder
            .Entity<Comment>()
            .HasOne(c => c.Activity)
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Comment>().HasIndex(c => new { c.ActivityId, c.Date });

        builder
            .Entity<ReadingSession>()
            .HasOne(r => r.LibraryEntry)
            .WithMany(e => e.Readings)
            .HasForeignKey(r => r.LibraryEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<ReadingSession>()
            .HasIndex(r => new { r.LibraryEntryId, r.FinishedDate })
            .IsUnique();

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
