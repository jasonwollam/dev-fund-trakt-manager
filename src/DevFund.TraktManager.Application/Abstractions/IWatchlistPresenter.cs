using DevFund.TraktManager.Domain.Entities;

namespace DevFund.TraktManager.Application.Abstractions;

public interface IWatchlistPresenter
{
    Task PresentAsync(IReadOnlyList<WatchlistEntry> entries, CancellationToken cancellationToken = default);
}
