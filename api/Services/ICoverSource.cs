namespace ReadersRealm.Api.Services;

public enum CoverLookup
{
    Found,
    NothingThere,
    Unavailable,
    Throttled,
}

public readonly record struct CoverResult(CoverLookup Outcome, string? Url)
{
    public static readonly CoverResult NothingThere = new(CoverLookup.NothingThere, null);
    public static readonly CoverResult Unavailable = new(CoverLookup.Unavailable, null);
    public static readonly CoverResult Throttled = new(CoverLookup.Throttled, null);

    public static CoverResult Found(string url) => new(CoverLookup.Found, url);

    public static CoverLookup Worse(CoverLookup left, CoverLookup right) =>
        Rank(left) >= Rank(right) ? left : right;

    public static CoverResult From(CoverLookup outcome) =>
        outcome switch
        {
            CoverLookup.Throttled => Throttled,
            CoverLookup.Unavailable => Unavailable,
            _ => NothingThere,
        };

    private static int Rank(CoverLookup outcome) =>
        outcome switch
        {
            CoverLookup.Throttled => 3,
            CoverLookup.Unavailable => 2,
            CoverLookup.NothingThere => 1,
            _ => 0,
        };
}

public interface ICoverSource
{
    Task<CoverResult> FindCoverAsync(
        string title,
        string author,
        string isbn,
        CancellationToken cancellationToken
    );
}
