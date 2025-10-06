using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;

namespace DevFund.TraktManager.Application.Services;

/// <summary>
/// Coordinates application and presentation layers for watchlist views.
/// </summary>
public sealed class WatchlistOrchestrator
{
    private readonly WatchlistService _watchlistService;
    private readonly IEnumerable<IWatchlistPresenter> _presenters;

    public WatchlistOrchestrator(WatchlistService watchlistService, IEnumerable<IWatchlistPresenter> presenters)
    {
        _watchlistService = watchlistService ?? throw new ArgumentNullException(nameof(watchlistService));
        _presenters = presenters?.ToArray() ?? throw new ArgumentNullException(nameof(presenters));
    }

    public async Task ExecuteAsync(WatchlistRequest request, CancellationToken cancellationToken = default)
    {
        var entries = await _watchlistService.GetWatchlistAsync(request, cancellationToken).ConfigureAwait(false);

        foreach (var presenter in _presenters)
        {
            await presenter.PresentAsync(entries, cancellationToken).ConfigureAwait(false);
        }
    }
}
