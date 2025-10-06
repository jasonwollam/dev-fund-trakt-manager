using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.Services;

namespace DevFund.TraktManager.Presentation.Cli.Presentation.Strategies;

public sealed class WatchlistCliCommandStrategy : ICliCommandStrategy
{
    private readonly WatchlistOrchestrator _orchestrator;

    public WatchlistCliCommandStrategy(WatchlistOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public bool CanHandle(CliOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Mode == CliMode.Watchlist;
    }

    public Task ExecuteAsync(CliOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        var watchlistOptions = options.Watchlist;
        var request = new WatchlistRequest(watchlistOptions.Filter, watchlistOptions.SortField, watchlistOptions.SortOrder);
        return _orchestrator.ExecuteAsync(request, cancellationToken);
    }
}
