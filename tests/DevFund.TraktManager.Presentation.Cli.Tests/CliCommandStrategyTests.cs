using System.Threading;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.Services;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;
using DevFund.TraktManager.Presentation.Cli;
using DevFund.TraktManager.Presentation.Cli.Presentation.Strategies;

namespace DevFund.TraktManager.Presentation.Cli.Tests;

public class CliCommandStrategyTests
{
    [Fact]
    public void CalendarStrategy_CanHandleCalendarMode()
    {
        var service = new CalendarService(new RecordingCalendarClient());
        var presenter = new RecordingCalendarPresenter();
        var orchestrator = new CalendarOrchestrator(service, new[] { presenter });
        var strategy = new CalendarCliCommandStrategy(orchestrator);
        var options = new CliOptions(
            CliMode.Calendar,
            new CalendarOptions(new DateOnly(2024, 1, 1), 7),
            new WatchlistOptions(WatchlistItemFilter.All, WatchlistSortField.Rank, WatchlistSortOrder.Asc),
            new ListsOptions(ListCollectionKind.Personal, "me", null, ListItemsType.All, includeItems: false, page: null, limit: null, SavedFilterSection.Movies));

        Assert.True(strategy.CanHandle(options));
    }

    [Fact]
    public async Task WatchlistStrategy_UsesCliOptions()
    {
        var client = new RecordingWatchlistClient();
        var service = new WatchlistService(client);
        var presenter = new RecordingWatchlistPresenter();
        var orchestrator = new WatchlistOrchestrator(service, new[] { presenter });
        var strategy = new WatchlistCliCommandStrategy(orchestrator);
        var options = new CliOptions(
            CliMode.Watchlist,
            new CalendarOptions(new DateOnly(2024, 1, 1), 7),
            new WatchlistOptions(WatchlistItemFilter.Shows, WatchlistSortField.Added, WatchlistSortOrder.Desc),
            new ListsOptions(ListCollectionKind.Personal, "me", null, ListItemsType.All, includeItems: false, page: null, limit: null, SavedFilterSection.Shows));

        await strategy.ExecuteAsync(options, System.Threading.CancellationToken.None);

        Assert.NotNull(client.LastRequest);
        Assert.Equal(WatchlistItemFilter.Shows, client.LastRequest!.ItemFilter);
        Assert.Equal(WatchlistSortField.Added, client.LastRequest.SortField);
        Assert.Equal(WatchlistSortOrder.Desc, client.LastRequest.SortOrder);
        Assert.NotNull(presenter.Entries);
    }

    [Fact]
    public async Task ListsStrategy_ConvertsCliOptionsIntoRequest()
    {
        var client = new RecordingListsClient();
        var service = new ListsService(client);
        var presenter = new RecordingListsPresenter();
        var orchestrator = new ListsOrchestrator(service, new[] { presenter });
        var strategy = new ListsCliCommandStrategy(orchestrator);
        var options = new CliOptions(
            CliMode.Lists,
            new CalendarOptions(new DateOnly(2024, 1, 1), 7),
            new WatchlistOptions(WatchlistItemFilter.All, WatchlistSortField.Rank, WatchlistSortOrder.Asc),
            new ListsOptions(ListCollectionKind.Liked, "user-slug", null, ListItemsType.Movies, includeItems: false, page: 2, limit: 25, SavedFilterSection.Movies));

        await strategy.ExecuteAsync(options, CancellationToken.None);

        Assert.NotNull(client.LastRequest);
        Assert.Equal(ListCollectionKind.Liked, client.LastRequest!.Kind);
        Assert.Equal("user-slug", client.LastRequest.UserSlug);
        Assert.Equal(ListItemsType.Movies, client.LastRequest.ItemType);
        Assert.Equal(2, client.LastRequest.Page);
        Assert.Equal(25, client.LastRequest.Limit);
        Assert.True(presenter.Invoked);
    }

    private sealed class RecordingCalendarClient : ITraktCalendarClient
    {
        public DateOnly? LastStartDate { get; private set; }
        public int? LastDays { get; private set; }

        public Task<IReadOnlyList<CalendarEntry>> GetMyShowsAsync(DateOnly startDate, int days, CancellationToken cancellationToken = default)
        {
            LastStartDate = startDate;
            LastDays = days;
            return Task.FromResult<IReadOnlyList<CalendarEntry>>(Array.Empty<CalendarEntry>());
        }
    }

    private sealed class RecordingCalendarPresenter : ICalendarPresenter
    {
        public Task PresentAsync(IReadOnlyList<CalendarEntry> entries, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingWatchlistClient : ITraktWatchlistClient
    {
        public WatchlistRequest? LastRequest { get; private set; }

        public Task<IReadOnlyList<WatchlistEntry>> GetWatchlistAsync(WatchlistRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult<IReadOnlyList<WatchlistEntry>>(Array.Empty<WatchlistEntry>());
        }
    }

    private sealed class RecordingWatchlistPresenter : IWatchlistPresenter
    {
        public IReadOnlyList<WatchlistEntry>? Entries { get; private set; }

        public Task PresentAsync(IReadOnlyList<WatchlistEntry> entries, CancellationToken cancellationToken = default)
        {
            Entries = entries;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingListsClient : ITraktListsClient
    {
        public ListsRequest? LastRequest { get; private set; }

        public Task<ListCollectionResult> GetListsAsync(ListsRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new ListCollectionResult(Array.Empty<ListCollectionItem>(), null));
        }

        public Task<SavedFiltersResult> GetSavedFiltersAsync(ListsRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SavedFiltersResult(Array.Empty<SavedFilter>(), null));
        }

        public Task<IReadOnlyList<ListItem>> GetListItemsAsync(ListItemsRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ListItem>>(Array.Empty<ListItem>());
        }

        public Task<UserList?> GetListDetailsAsync(string userSlug, string listSlug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserList?>(null);
        }
    }

    private sealed class RecordingListsPresenter : IListsPresenter
    {
        public bool Invoked { get; private set; }

        public Task PresentAsync(ListsResponse response, CancellationToken cancellationToken = default)
        {
            Invoked = true;
            return Task.CompletedTask;
        }
    }
}
