namespace ReadersRealm.Api.Import;

public class ImportedBook
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Shelf { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
    public bool ContainsSpoiler { get; set; }
    public DateTime? ReadDate { get; set; }
    public DateTime? AddedDate { get; set; }
}

public class SkippedRow
{
    public int Line { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ImportParseResult
{
    public List<ImportedBook> Books { get; set; } = [];
    public List<SkippedRow> Skipped { get; set; } = [];
    public string? Error { get; set; }
}

public interface IBookImportParser
{
    string Service { get; }
    bool CanParse(string[] header);
    ImportParseResult Parse(List<string[]> rows);
}
