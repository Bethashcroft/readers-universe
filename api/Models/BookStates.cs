namespace ReadersRealm.Api.Models;

public static class BookShelf
{
    public const string CurrentlyReading = "currently-reading";
    public const string Read = "read";
    public const string Tbr = "tbr";
    public const string WantToRead = "want-to-read";
    public const string Dnf = "dnf";

    public static readonly string[] All = [CurrentlyReading, Read, Tbr, WantToRead, Dnf];
}

public static class BookOffer
{
    public const string None = "none";
    public const string AvailableToBorrow = "available-to-borrow";
    public const string LentOut = "lent-out";
    public const string ForSale = "for-sale";

    public static readonly string[] All = [None, AvailableToBorrow, LentOut, ForSale];
}

public static class BorrowStatus
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Declined = "declined";
}

public static class LibrarySort
{
    public const string Added = "added";
    public const string Title = "title";
    public const string Author = "author";
    public const string Rating = "rating";

    public static readonly string[] All = [Added, Title, Author, Rating];
}

public static class FollowStates
{
    public const string None = "none";
    public const string Requested = "requested";
    public const string Following = "following";
    public const string Self = "self";
}

public static class NotificationTypes
{
    public const string FollowRequested = "follow-requested";
    public const string FollowApproved = "follow-approved";
    public const string NewFollower = "new-follower";
    public const string Trusted = "trusted";
}
