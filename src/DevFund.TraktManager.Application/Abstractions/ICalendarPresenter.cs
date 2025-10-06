using DevFund.TraktManager.Domain.Entities;

namespace DevFund.TraktManager.Application.Abstractions;

/// <summary>
/// Presentation boundary for rendering calendar entries to a user or integration surface.
/// </summary>
public interface ICalendarPresenter
{
    Task PresentAsync(IReadOnlyList<CalendarEntry> entries, CancellationToken cancellationToken = default);
}
