using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.Services;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Application.Tests;

public class WatchlistServiceTests
{
    [Fact]
    public async Task GetWatchlistAsync_ForwardsToClient()
    {
        var request = new WatchlistRequest();
        var expectedEntries = CreateEntries();
        var client = new StubWatchlistClient(expectedEntries);
        var service = new WatchlistService(client);

        var result = await service.GetWatchlistAsync(request);

        Assert.Equal(expectedEntries, result);
    }

    [Fact]
    public async Task WatchlistOrchestrator_PresentsResults()
    {
        var request = new WatchlistRequest();
        var expectedEntries = CreateEntries();
        var client = new StubWatchlistClient(expectedEntries);
        var service = new WatchlistService(client);
        var presenter = new RecordingWatchlistPresenter();
        var orchestrator = new WatchlistOrchestrator(service, new[] { presenter });

        await orchestrator.ExecuteAsync(request);

        Assert.Equal(expectedEntries, presenter.Entries);
    }

    private static IReadOnlyList<WatchlistEntry> CreateEntries()
    {
        var ids = new TraktIds(1, "movie");
        var movie = new Movie("Example", 2020, ids);
        var entry = new WatchlistEntry(1, 10, DateTimeOffset.UtcNow, WatchlistItemType.Movie, movie: movie);
        return new List<WatchlistEntry> { entry };
    }

    private sealed class StubWatchlistClient : ITraktWatchlistClient
    {
        private readonly IReadOnlyList<WatchlistEntry> _entries;

        public StubWatchlistClient(IReadOnlyList<WatchlistEntry> entries)
        {
            _entries = entries;
        }

        public Task<IReadOnlyList<WatchlistEntry>> GetWatchlistAsync(WatchlistRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_entries);
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
