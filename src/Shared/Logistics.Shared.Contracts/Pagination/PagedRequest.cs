namespace Logistics.Shared.Contracts.Pagination;

/// <summary>
/// Standard pagination request parameters.
/// </summary>
public class PagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}
