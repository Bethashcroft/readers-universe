namespace ReadersRealm.Api.Models;

public static class AuthorNames
{
    private static readonly HashSet<string> Endings = new(StringComparer.OrdinalIgnoreCase)
    {
        "jr",
        "jr.",
        "sr",
        "sr.",
        "ii",
        "iii",
        "iv",
    };

    private static readonly HashSet<string> SurnamePrefixes = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "al",
        "bin",
        "da",
        "das",
        "de",
        "del",
        "della",
        "den",
        "der",
        "des",
        "di",
        "dos",
        "du",
        "ibn",
        "la",
        "le",
        "st.",
        "ten",
        "ter",
        "van",
        "von",
    };

    public static string SortKey(string author)
    {
        var words = author
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => word.TrimEnd(','))
            .Where(word => word.Length > 0)
            .ToArray();

        var end = words.Length;

        while (end > 1 && Endings.Contains(words[end - 1]))
        {
            end--;
        }

        if (end == 0)
        {
            return string.Empty;
        }

        var start = end - 1;

        while (start > 1 && SurnamePrefixes.Contains(words[start - 1]))
        {
            start--;
        }

        return string.Join(' ', words[start..end].Concat(words[..start]).Concat(words[end..]))
            .ToLowerInvariant();
    }
}
