using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;

namespace DevFund.TraktManager.Application.Services;

/// <summary>
/// Coordinates application and presentation layers for calendar views.
/// </summary>
public sealed class CalendarOrchestrator
{
    private readonly CalendarService _calendarService;
    private readonly IEnumerable<ICalendarPresenter> _presenters;

    public CalendarOrchestrator(CalendarService calendarService, IEnumerable<ICalendarPresenter> presenters)
    {
        _calendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
        _presenters = presenters?.ToArray() ?? throw new ArgumentNullException(nameof(presenters));
    }

    public async Task ExecuteAsync(CalendarRequest request, CancellationToken cancellationToken = default)
    {
        var entries = await _calendarService.GetUpcomingShowsAsync(request, cancellationToken).ConfigureAwait(false);

        foreach (var presenter in _presenters)
        {
            await presenter.PresentAsync(entries, cancellationToken).ConfigureAwait(false);
        }
    }
}
