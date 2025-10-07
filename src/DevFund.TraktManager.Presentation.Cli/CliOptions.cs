using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Presentation.Cli;

public enum CliMode
{
    Calendar,
    Watchlist,
    Lists
}

public sealed class CalendarOptions
{
    public CalendarOptions(DateOnly startDate, int days)
    {
        if (days <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, "Days must be positive.");
        }

        StartDate = startDate;
        Days = days;
    }

    public DateOnly StartDate { get; }

    public int Days { get; }
}

public sealed class WatchlistOptions
{
    public WatchlistOptions(WatchlistItemFilter filter, WatchlistSortField sortField, WatchlistSortOrder sortOrder)
    {
        Filter = filter;
        SortField = sortField;
        SortOrder = sortOrder;
    }

    public WatchlistItemFilter Filter { get; }

    public WatchlistSortField SortField { get; }

    public WatchlistSortOrder SortOrder { get; }
}

public sealed class ListsOptions
{
    public ListsOptions(
        ListCollectionKind kind,
        string? userSlug,
        string? listSlug,
        ListItemsType itemType,
        bool includeItems,
        int? page,
        int? limit,
        SavedFilterSection section)
    {
        Kind = kind;
        UserSlug = string.IsNullOrWhiteSpace(userSlug) ? "me" : userSlug.Trim();
        ListSlug = string.IsNullOrWhiteSpace(listSlug) ? null : listSlug.Trim();
        ItemType = itemType;
        IncludeItems = includeItems;
        Page = page;
        Limit = limit;
        Section = section;
    }

    public ListCollectionKind Kind { get; }

    public string UserSlug { get; }

    public string? ListSlug { get; }

    public ListItemsType ItemType { get; }

    public bool IncludeItems { get; }

    public int? Page { get; }

    public int? Limit { get; }

    public SavedFilterSection Section { get; }
}

public sealed class CliOptions
{
    public CliOptions(CliMode mode, CalendarOptions calendar, WatchlistOptions watchlist, ListsOptions lists)
    {
        Mode = mode;
        Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        Watchlist = watchlist ?? throw new ArgumentNullException(nameof(watchlist));
        Lists = lists ?? throw new ArgumentNullException(nameof(lists));
    }

    public CliMode Mode { get; }

    public CalendarOptions Calendar { get; }

    public WatchlistOptions Watchlist { get; }

    public ListsOptions Lists { get; }

    public static CliOptions CreateDefault()
    {
        var defaultCalendar = new CalendarOptions(DateOnly.FromDateTime(DateTime.UtcNow), 7);
        var defaultWatchlist = new WatchlistOptions(WatchlistItemFilter.All, WatchlistSortField.Rank, WatchlistSortOrder.Asc);
        var defaultLists = new ListsOptions(ListCollectionKind.Personal, "me", null, ListItemsType.All, includeItems: false, page: null, limit: null, SavedFilterSection.Movies);
        return new CliOptions(CliMode.Calendar, defaultCalendar, defaultWatchlist, defaultLists);
    }
}
