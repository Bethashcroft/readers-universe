using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Services;

public static class CoverPolicy
{
    public const string PlaceholderHost = "placehold.co";

    private const int MaxTitleInPlaceholder = 120;

    public static string PlaceholderFor(string title)
    {
        var cut = MaxTitleInPlaceholder;

        if (title.Length > cut)
        {
            if (char.IsHighSurrogate(title[cut - 1]))
            {
                cut--;
            }
        }
        else
        {
            cut = title.Length;
        }

        var trimmed = title[..cut];

        return $"https://{PlaceholderHost}/200x300/1a1430/a9a3cc?text={Uri.EscapeDataString(trimmed)}";
    }

    public static bool IsPlaceholder(string coverUrl) =>
        Uri.TryCreate(coverUrl, UriKind.Absolute, out var uri) && uri.Host == PlaceholderHost;

    public static bool ShouldReplaceCover(Book book, string? candidateUrl, bool speculative)
    {
        if (string.IsNullOrEmpty(candidateUrl) || IsPlaceholder(candidateUrl))
        {
            return false;
        }

        if (book.CoverUrl.Length > 0 && !IsPlaceholder(book.CoverUrl))
        {
            return false;
        }

        return !speculative || book.CoverCheckedAt == null;
    }

    public static void ApplyCover(Book book, string coverUrl)
    {
        book.CoverUrl = coverUrl;
        book.CoverCheckedAt = null;
    }
}
