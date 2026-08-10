namespace ReadersRealm.Api.Controllers;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }

    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 100;

    public static (int page, int pageSize) Normalise(int? page, int? pageSize)
    {
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        return (Math.Max(page ?? 1, 1), size);
    }

    public static PagedResult<T> From(List<T> items, int page, int pageSize, int total) =>
        new()
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize),
        };
}
