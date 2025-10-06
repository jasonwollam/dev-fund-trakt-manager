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
        var options = new CliOptions(CliMode.Calendar, new CalendarOptions(new DateOnly(2024, 1, 1), 7), new WatchlistOptions(WatchlistItemFilter.All, WatchlistSortField.Rank, WatchlistSortOrder.Asc));

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
            new WatchlistOptions(WatchlistItemFilter.Shows, WatchlistSortField.Added, WatchlistSortOrder.Desc));

    await strategy.ExecuteAsync(options, System.Threading.CancellationToken.None);

        Assert.NotNull(client.LastRequest);
        Assert.Equal(WatchlistItemFilter.Shows, client.LastRequest!.ItemFilter);
        Assert.Equal(WatchlistSortField.Added, client.LastRequest.SortField);
        Assert.Equal(WatchlistSortOrder.Desc, client.LastRequest.SortOrder);
        Assert.NotNull(presenter.Entries);
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
}
