using DevFund.TraktManager.Application.Contracts;

namespace DevFund.TraktManager.Presentation.Cli;

public enum CliMode
{
    Calendar,
    Watchlist
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

public sealed class CliOptions
{
    public CliOptions(CliMode mode, CalendarOptions calendar, WatchlistOptions watchlist)
    {
        Mode = mode;
        Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        Watchlist = watchlist ?? throw new ArgumentNullException(nameof(watchlist));
    }

    public CliMode Mode { get; }

    public CalendarOptions Calendar { get; }

    public WatchlistOptions Watchlist { get; }

    public static CliOptions CreateDefault()
    {
        var defaultCalendar = new CalendarOptions(DateOnly.FromDateTime(DateTime.UtcNow), 7);
        var defaultWatchlist = new WatchlistOptions(WatchlistItemFilter.All, WatchlistSortField.Rank, WatchlistSortOrder.Asc);
        return new CliOptions(CliMode.Calendar, defaultCalendar, defaultWatchlist);
    }
}
