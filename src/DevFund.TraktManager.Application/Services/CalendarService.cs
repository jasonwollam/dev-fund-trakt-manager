using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;

namespace DevFund.TraktManager.Application.Services;

/// <summary>
/// Core application service responsible for retrieving upcoming show information.
/// </summary>
public sealed class CalendarService
{
    private readonly ITraktCalendarClient _calendarClient;

    public CalendarService(ITraktCalendarClient calendarClient)
    {
        _calendarClient = calendarClient ?? throw new ArgumentNullException(nameof(calendarClient));
    }

    public Task<IReadOnlyList<CalendarEntry>> GetUpcomingShowsAsync(CalendarRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _calendarClient.GetMyShowsAsync(request.StartDate, request.Days, cancellationToken);
    }
}
