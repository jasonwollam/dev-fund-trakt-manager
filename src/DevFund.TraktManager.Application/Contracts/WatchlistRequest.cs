namespace DevFund.TraktManager.Application.Contracts;

public enum WatchlistItemFilter
{
    All,
    Movies,
    Shows,
    Seasons,
    Episodes
}

public enum WatchlistSortField
{
    Rank,
    Added,
    Title,
    Released,
    Runtime,
    Popularity,
    Random,
    Percentage,
    ImdbRating,
    TmdbRating,
    RtTomatometer,
    RtAudience,
    Metascore,
    Votes,
    ImdbVotes,
    TmdbVotes,
    MyRating,
    Watched,
    Collected
}

public enum WatchlistSortOrder
{
    Asc,
    Desc
}

public sealed class WatchlistRequest
{
    public WatchlistRequest(
        WatchlistItemFilter itemFilter = WatchlistItemFilter.All,
        WatchlistSortField sortField = WatchlistSortField.Rank,
        WatchlistSortOrder sortOrder = WatchlistSortOrder.Asc)
    {
        ItemFilter = itemFilter;
        SortField = sortField;
        SortOrder = sortOrder;
    }

    public WatchlistItemFilter ItemFilter { get; }

    public WatchlistSortField SortField { get; }

    public WatchlistSortOrder SortOrder { get; }
}
