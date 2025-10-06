using DevFund.TraktManager.Domain.Entities;

namespace DevFund.TraktManager.Application.Abstractions;

/// <summary>
/// Abstraction over the persistence boundary that retrieves calendar information from Trakt.
/// </summary>
public interface ITraktCalendarClient
{
    Task<IReadOnlyList<CalendarEntry>> GetMyShowsAsync(DateOnly startDate, int days, CancellationToken cancellationToken = default);
}
