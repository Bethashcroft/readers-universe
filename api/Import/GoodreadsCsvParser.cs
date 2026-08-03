using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Import;

public partial class GoodreadsCsvParser : IBookImportParser
{
    public string Service => "Goodreads";

    private static readonly string[] RequiredColumns = ["Title", "Author", "Exclusive Shelf"];

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakTag();

    public bool CanParse(string[] header) => RequiredColumns.All(header.Contains);

    public ImportParseResult Parse(List<string[]> rows)
    {
        var result = new ImportParseResult();

        if (rows.Count == 0)
        {
            result.Error = "That file looks empty.";
            return result;
        }

        var header = rows[0];

        if (!CanParse(header))
        {
            result.Error =
                "That does not look like a Goodreads export. Make sure you upload the CSV from Goodreads, My Books, Import and Export.";
            return result;
        }

        var index = header
            .Select((name, i) => (name, i))
            .GroupBy(x => x.name)
            .ToDictionary(g => g.Key, g => g.First().i);

        for (var line = 1; line < rows.Count; line++)
        {
            var row = rows[line];

            if (row.Length < header.Length && row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            string Field(string name) =>
                index.TryGetValue(name, out var i) && i < row.Length ? row[i].Trim() : string.Empty;

            var title = Clean(Field("Title"));
            var author = Clean(Field("Author"));

            if (title.Length == 0)
            {
                result.Skipped.Add(new SkippedRow { Line = line + 1, Reason = "No title" });
                continue;
            }

            var review = ReadReview(Field("My Review"));
            var rating = ReadRating(Field("My Rating"));

            result.Books.Add(
                new ImportedBook
                {
                    Title = Truncate(title, 300),
                    Author = Truncate(author, 200),
                    Isbn = Truncate(ReadIsbn(Field("ISBN13"), Field("ISBN")), 20),
                    Shelf = ReadShelf(Field("Exclusive Shelf")),
                    Rating = rating,
                    ReviewText = Truncate(review, 2000),
                    ContainsSpoiler = Field("Spoiler").Equals("true", StringComparison.OrdinalIgnoreCase),
                    ReadDate = ReadDate(Field("Date Read")),
                    AddedDate = ReadDate(Field("Date Added")),
                }
            );
        }

        if (result.Books.Count == 0 && result.Error == null)
        {
            result.Error = "No books were found in that file.";
        }

        return result;
    }

    private static string Clean(string value) => WhitespaceRun().Replace(value, " ").Trim();

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string ReadReview(string value)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var withBreaks = LineBreakTag().Replace(value, "\n");
        return WebUtility.HtmlDecode(withBreaks).Trim();
    }

    private static int? ReadRating(string value)
    {
        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return null;
        }

        var rounded = (int)Math.Round(parsed);
        return rounded is >= 1 and <= 5 ? rounded : null;
    }

    private static string ReadIsbn(string isbn13, string isbn)
    {
        var preferred = Unwrap(isbn13);
        return preferred.Length > 0 ? preferred : Unwrap(isbn);
    }

    private static string Unwrap(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.StartsWith("=\"", StringComparison.Ordinal) && trimmed.EndsWith('"'))
        {
            trimmed = trimmed[2..^1];
        }

        return new string(trimmed.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string ReadShelf(string value) =>
        value.ToLowerInvariant() switch
        {
            "read" => BookShelf.Read,
            "currently-reading" => BookShelf.CurrentlyReading,
            "did-not-finish" or "dnf" or "abandoned" => BookShelf.Dnf,
            "tbr" or "to-be-read" => BookShelf.Tbr,
            _ => BookShelf.WantToRead,
        };

    private static DateTime? ReadDate(string value)
    {
        if (
            DateTime.TryParseExact(
                value,
                "yyyy/MM/dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed
            )
        )
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        return null;
    }
}
