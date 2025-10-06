using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;

namespace DevFund.TraktManager.Application.Services;

/// <summary>
/// Core application service responsible for retrieving the user's watchlist.
/// </summary>
public sealed class WatchlistService
{
    private readonly ITraktWatchlistClient _watchlistClient;

    public WatchlistService(ITraktWatchlistClient watchlistClient)
    {
        _watchlistClient = watchlistClient ?? throw new ArgumentNullException(nameof(watchlistClient));
    }

    public Task<IReadOnlyList<WatchlistEntry>> GetWatchlistAsync(WatchlistRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _watchlistClient.GetWatchlistAsync(request, cancellationToken);
    }
}
