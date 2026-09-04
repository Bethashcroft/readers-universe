using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;

namespace ReadersRealm.Api.Services;

public class CoverBackfillResult
{
    public int Checked { get; set; }
    public int Fixed { get; set; }
    public int AlreadyFine { get; set; }
    public int NotFound { get; set; }
    public int Unverifiable { get; set; }
    public int Unreachable { get; set; }
    public int NextAfterId { get; set; }
    public int Total { get; set; }
    public bool Done { get; set; }
    public bool Throttled { get; set; }
}

internal enum CoverCheck
{
    Good,
    Missing,
    Unverifiable,
    Unknown,
    Throttled,
}

public class CoverService(
    AppDbContext context,
    IEnumerable<ICoverSource> sources,
    HttpClient http,
    ILogger<CoverService> logger
)
{
    private const int MinimumImageBytes = 1000;

    private static readonly TimeSpan RecheckAfter = TimeSpan.FromDays(30);

    private static readonly string[] VerifiableHosts = ["covers.openlibrary.org"];

    private readonly AppDbContext _context = context;
    private readonly IEnumerable<ICoverSource> _sources = sources;
    private readonly HttpClient _http = http;
    private readonly ILogger<CoverService> _logger = logger;

    private IQueryable<Models.Book> Candidates(string userId, DateTime cutoff, int afterId) =>
        _context
            .LibraryEntries.Where(e => e.UserId == userId)
            .Select(e => e.Book)
            .Distinct()
            .Where(b =>
                b.Id > afterId && (b.CoverCheckedAt == null || b.CoverCheckedAt < cutoff)
            )
            .OrderBy(b => b.Id);

    public async Task<CoverBackfillResult> BackfillAsync(
        string userId,
        int afterId,
        int max,
        bool withTotal,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var cutoff = now - RecheckAfter;
        var result = new CoverBackfillResult { NextAfterId = afterId };

        if (withTotal)
        {
            result.Total = await Candidates(userId, cutoff, 0).CountAsync(cancellationToken);
        }

        var batch = await Candidates(userId, cutoff, afterId)
            .Take(max)
            .ToListAsync(cancellationToken);

        using var gate = new SemaphoreSlim(4);

        var lookups = await Task.WhenAll(
            batch.Select(async book =>
            {
                await gate.WaitAsync(cancellationToken);

                try
                {
                    var state = await CheckAsync(book.CoverUrl, cancellationToken);

                    if (state != CoverCheck.Missing)
                    {
                        return (book, found: (string?)null, state);
                    }

                    var lookup = await FindAsync(
                        book.Title,
                        book.Author,
                        book.Isbn,
                        cancellationToken
                    );

                    if (lookup.Outcome == CoverLookup.Throttled)
                    {
                        return (book, found: (string?)null, state: CoverCheck.Throttled);
                    }

                    if (lookup.Outcome == CoverLookup.Unavailable)
                    {
                        return (book, found: (string?)null, state: CoverCheck.Unknown);
                    }

                    if (lookup.Outcome == CoverLookup.NothingThere || lookup.Url == null)
                    {
                        return (book, found: (string?)null, state);
                    }

                    var confirmed = await CheckAsync(lookup.Url, cancellationToken);

                    return confirmed switch
                    {
                        CoverCheck.Good or CoverCheck.Unverifiable => (
                            book,
                            found: lookup.Url,
                            state
                        ),
                        CoverCheck.Missing => (book, found: (string?)null, state),
                        _ => (book, found: (string?)null, state: confirmed),
                    };
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(
                        ex,
                        "Cover lookup failed for book {BookId} ({Title})",
                        book.Id,
                        book.Title
                    );

                    return (book, found: (string?)null, state: CoverCheck.Unknown);
                }
                finally
                {
                    gate.Release();
                }
            })
        );

        foreach (var (book, found, state) in lookups)
        {
            result.Checked++;

            switch (state)
            {
                case CoverCheck.Throttled:
                    result.Throttled = true;
                    result.Unreachable++;
                    break;

                case CoverCheck.Unknown:
                    result.Unreachable++;
                    break;

                case CoverCheck.Unverifiable:
                    result.Unverifiable++;
                    book.CoverCheckedAt = now;
                    break;

                case CoverCheck.Good:
                    result.AlreadyFine++;
                    book.CoverCheckedAt = now;
                    break;

                case CoverCheck.Missing when found != null:
                    book.CoverUrl = found;
                    book.CoverCheckedAt = now;
                    result.Fixed++;
                    break;

                case CoverCheck.Missing:
                    result.NotFound++;

                    if (!CoverPolicy.IsPlaceholder(book.CoverUrl))
                    {
                        book.CoverUrl = CoverPolicy.PlaceholderFor(book.Title);
                    }

                    book.CoverCheckedAt = now;
                    break;

                default:
                    result.Unreachable++;
                    break;
            }
        }

        var settled = result.Checked - result.Unreachable;

        if (settled > 0)
        {
            await _context.SaveChangesAsync(CancellationToken.None);
        }

        if (batch.Count > 0)
        {
            result.NextAfterId = batch[^1].Id;
        }

        result.Done = result.Throttled || batch.Count < max;

        return result;
    }

    private async Task<CoverResult> FindAsync(
        string title,
        string author,
        string isbn,
        CancellationToken cancellationToken
    )
    {
        var worst = CoverLookup.NothingThere;

        foreach (var source in _sources)
        {
            var result = await source.FindCoverAsync(title, author, isbn, cancellationToken);

            if (result.Outcome == CoverLookup.Found)
            {
                return result;
            }

            worst = CoverResult.Worse(worst, result.Outcome);
        }

        return CoverResult.From(worst);
    }

    private async Task<CoverCheck> CheckAsync(string url, CancellationToken cancellationToken)
    {
        if (url.Length == 0)
        {
            return CoverCheck.Missing;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return CoverCheck.Unverifiable;
        }

        if (uri.Host == CoverPolicy.PlaceholderHost)
        {
            return CoverCheck.Missing;
        }

        if (uri.Scheme != Uri.UriSchemeHttps || !VerifiableHosts.Contains(uri.Host))
        {
            return CoverCheck.Unverifiable;
        }

        try
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, uri);
            using var probe = await _http.SendAsync(
                head,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            if (
                probe.IsSuccessStatusCode
                && probe.Content.Headers.ContentLength is { } size
                && size > MinimumImageBytes
            )
            {
                return CoverCheck.Good;
            }

            if (probe.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return CoverCheck.Missing;
            }

            using var response = await _http.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return CoverCheck.Missing;
            }

            if (
                response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                || response.StatusCode == System.Net.HttpStatusCode.Forbidden
            )
            {
                _logger.LogWarning(
                    "Open Library is throttling us ({Status}) for {Url}",
                    response.StatusCode,
                    uri
                );

                return CoverCheck.Throttled;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Could not verify cover {Url}: {Status}",
                    uri,
                    response.StatusCode
                );

                return CoverCheck.Unknown;
            }

            var declared = response.Content.Headers.ContentLength;

            if (declared != null)
            {
                return declared > MinimumImageBytes ? CoverCheck.Good : CoverCheck.Missing;
            }

            var buffer = new byte[MinimumImageBytes + 1];
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var read = await ReadUpToAsync(stream, buffer, cancellationToken);

            return read > MinimumImageBytes ? CoverCheck.Good : CoverCheck.Missing;
        }
        catch (Exception ex)
            when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            _logger.LogInformation(ex, "Could not reach {Url} to verify a cover", uri);

            return CoverCheck.Unknown;
        }
    }

    private static async Task<int> ReadUpToAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken
    )
    {
        var total = 0;

        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(
                buffer.AsMemory(total, buffer.Length - total),
                cancellationToken
            );

            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }
}
