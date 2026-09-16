using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReadersRealm.Api.Services;

public static partial class OpenLibraryMatching
{
    [GeneratedRegex(@"\s*\([^)]*\)\s*$")]
    private static partial Regex TrailingSeries();

    public static string BareTitle(string title) =>
        TrailingSeries().Replace(title, string.Empty).Trim();

    public static bool TitleMatches(JsonElement doc, string wantedTitle)
    {
        var candidate = doc.TryGetProperty("title", out var title)
            ? Normalise(title.GetString() ?? string.Empty)
            : string.Empty;

        return candidate.Length > 0 && candidate == wantedTitle;
    }

    public static bool AuthorMatches(JsonElement doc, string wantedAuthor)
    {
        if (
            !doc.TryGetProperty("author_name", out var authors)
            || authors.ValueKind != JsonValueKind.Array
        )
        {
            return false;
        }

        foreach (var author in authors.EnumerateArray())
        {
            if (Normalise(author.GetString() ?? string.Empty) == wantedAuthor)
            {
                return true;
            }
        }

        return false;
    }

    public static string Normalise(string value)
    {
        var builder = new StringBuilder();

        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }
}
