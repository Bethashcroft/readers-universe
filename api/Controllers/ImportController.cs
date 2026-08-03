using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadersRealm.Api.Import;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/library/import")]
[Authorize]
public class ImportController(
    LibraryImportService importService,
    IEnumerable<IBookImportParser> parsers
) : ControllerBase
{
    private const long MaxFileBytes = 5 * 1024 * 1024;

    private readonly LibraryImportService _importService = importService;
    private readonly IEnumerable<IBookImportParser> _parsers = parsers;

    [HttpPost]
    [RequestSizeLimit(MaxFileBytes)]
    public async Task<IActionResult> Import(IFormFile? file, [FromQuery] bool preview = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Choose a CSV file to import." });
        }

        if (file.Length > MaxFileBytes)
        {
            return BadRequest(new { message = "That file is too big. The limit is 5MB." });
        }

        List<string[]> rows;

        using (var stream = file.OpenReadStream())
        {
            rows = CsvReader.Read(stream);
        }

        if (rows.Count == 0)
        {
            return BadRequest(new { message = "That file looks empty." });
        }

        var parser = _parsers.FirstOrDefault(p => p.CanParse(rows[0]));

        if (parser == null)
        {
            return BadRequest(
                new
                {
                    message =
                        "We could not recognise that file. Upload the CSV from Goodreads, My Books, Import and Export.",
                }
            );
        }

        var parsed = parser.Parse(rows);

        if (parsed.Error != null)
        {
            return BadRequest(new { message = parsed.Error });
        }

        var outcome = await _importService.ApplyAsync(parsed.Books, userId!, commit: !preview);

        return Ok(
            new ImportSummaryResponse
            {
                Service = parser.Service,
                Committed = !preview,
                RowsFound = parsed.Books.Count,
                Added = outcome.Added,
                AlreadyOnShelves = outcome.AlreadyOnShelves,
                ReviewsAdded = outcome.ReviewsAdded,
                NewToCatalogue = outcome.NewToCatalogue,
                SkippedRows = parsed.Skipped.Count,
                ByShelf = outcome.ByShelf,
                Sample = parsed.Books.Take(5).Select(b => $"{b.Title} by {b.Author}").ToList(),
            }
        );
    }
}

public class ImportSummaryResponse
{
    public string Service { get; set; } = string.Empty;
    public bool Committed { get; set; }
    public int RowsFound { get; set; }
    public int Added { get; set; }
    public int AlreadyOnShelves { get; set; }
    public int ReviewsAdded { get; set; }
    public int NewToCatalogue { get; set; }
    public int SkippedRows { get; set; }
    public Dictionary<string, int> ByShelf { get; set; } = [];
    public List<string> Sample { get; set; } = [];
}
