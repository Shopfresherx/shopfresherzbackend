namespace ShopFresherz.Application.Common;

/// <summary>A paginated result wrapper returned by list query handlers.</summary>
/// <typeparam name="T">The item type in the page.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Gets the items on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Gets the total count of items across all pages.</summary>
    public int TotalCount { get; }

    /// <summary>Gets the current page number (1-based).</summary>
    public int Page { get; }

    /// <summary>Gets the number of items per page.</summary>
    public int PageSize { get; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Gets a value indicating whether there is a previous page.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Gets a value indicating whether there is a next page.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Initialises a new paged result.</summary>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>Creates a <see cref="PagedResult{T}"/> from a tuple returned by repository methods.</summary>
    public static PagedResult<T> From(
        (IReadOnlyList<T> Items, int TotalCount) source,
        int page,
        int pageSize) =>
        new(source.Items, source.TotalCount, page, pageSize);
}
