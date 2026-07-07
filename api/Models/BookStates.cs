namespace ReadersRealm.Api.Models;

public static class BookShelf
{
    public const string CurrentlyReading = "currently-reading";
    public const string Read = "read";
    public const string Tbr = "tbr";
    public const string Dnf = "dnf";

    public static readonly string[] All = [CurrentlyReading, Read, Tbr, Dnf];
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
