namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Paginated response for library items.
/// </summary>
/// <typeparam name="T">The type of library item.</typeparam>
public record LibraryItemListResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public LibraryItemListResponse()
    {
    }

    public LibraryItemListResponse(List<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static LibraryItemListResponse<T> Create(List<T> items, int page, int pageSize, int totalCount)
    {
        return new LibraryItemListResponse<T>(items, page, pageSize, totalCount);
    }
}
