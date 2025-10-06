using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.Services;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Application.Tests;

public class CalendarServiceTests
{
    [Fact]
    public async Task GetUpcomingShowsAsync_DelegatesToCalendarClient()
    {
        var expected = new[]
        {
            new CalendarEntry(new DateOnly(2024, 01, 01), new Show("Test", 2024, new TraktIds(1, "show")), new Episode(1, 1, "Pilot", new TraktIds(2, "episode")))
        };

        var client = new StubCalendarClient(expected);
        var service = new CalendarService(client);
        var result = await service.GetUpcomingShowsAsync(new CalendarRequest(new DateOnly(2024, 01, 01), 7));

        Assert.Single(result);
    }
}

public class CalendarOrchestratorTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsResultsToPresenters()
    {
        var entries = new[]
        {
            new CalendarEntry(new DateOnly(2024, 01, 01), new Show("Test", 2024, new TraktIds(1, "show")), new Episode(1, 1, "Pilot", new TraktIds(2, "episode")))
        };

        var client = new StubCalendarClient(entries);
        var service = new CalendarService(client);
        var presenter = new RecordingPresenter();
        var orchestrator = new CalendarOrchestrator(service, new[] { presenter });

        await orchestrator.ExecuteAsync(new CalendarRequest(new DateOnly(2024, 01, 01), 7));

        Assert.Equal(entries, presenter.Entries);
    }

    private sealed class RecordingPresenter : ICalendarPresenter
    {
        public IReadOnlyList<CalendarEntry>? Entries { get; private set; }

        public Task PresentAsync(IReadOnlyList<CalendarEntry> entries, CancellationToken cancellationToken = default)
        {
            Entries = entries;
            return Task.CompletedTask;
        }
    }
}

internal sealed class StubCalendarClient : ITraktCalendarClient
{
    private readonly IReadOnlyList<CalendarEntry> _entries;

    public StubCalendarClient(IReadOnlyList<CalendarEntry> entries)
    {
        _entries = entries;
    }

    public Task<IReadOnlyList<CalendarEntry>> GetMyShowsAsync(DateOnly startDate, int days, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_entries);
    }
}
