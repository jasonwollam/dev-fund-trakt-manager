using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;

namespace DevFund.TraktManager.Application.Abstractions;

public interface ITraktWatchlistClient
{
    Task<IReadOnlyList<WatchlistEntry>> GetWatchlistAsync(WatchlistRequest request, CancellationToken cancellationToken = default);
}
