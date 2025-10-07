namespace DevFund.TraktManager.Domain.ValueObjects;

/// <summary>
/// Captures pagination headers returned by Trakt endpoints.
/// </summary>
public sealed class PaginationMetadata
{
    public PaginationMetadata(int page, int limit, int pageCount, int itemCount)
    {
        if (page <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "Page must be positive.");
        }

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be positive.");
        }

        if (pageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), pageCount, "Page count cannot be negative.");
        }

        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), itemCount, "Item count cannot be negative.");
        }

        Page = page;
        Limit = limit;
        PageCount = pageCount;
        ItemCount = itemCount;
    }

    public int Page { get; }

    public int Limit { get; }

    public int PageCount { get; }

    public int ItemCount { get; }
}