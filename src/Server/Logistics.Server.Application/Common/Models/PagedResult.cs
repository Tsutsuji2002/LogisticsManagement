using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Common.Models;

/// <summary>
/// Represents an application-level paginated result set.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public PagedResponse<T> ToPagedResponse() => new()
    {
        Items = Items,
        TotalCount = TotalCount,
        Page = Page,
        PageSize = PageSize,
        TotalPages = TotalPages,
        HasPreviousPage = HasPreviousPage,
        HasNextPage = HasNextPage
    };
}
