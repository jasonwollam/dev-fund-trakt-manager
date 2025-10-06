using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.Services;

namespace DevFund.TraktManager.Presentation.Cli.Presentation.Strategies;

public sealed class CalendarCliCommandStrategy : ICliCommandStrategy
{
    private readonly CalendarOrchestrator _orchestrator;

    public CalendarCliCommandStrategy(CalendarOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public bool CanHandle(CliOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Mode == CliMode.Calendar;
    }

    public Task ExecuteAsync(CliOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        var calendarOptions = options.Calendar;
        var request = new CalendarRequest(calendarOptions.StartDate, calendarOptions.Days);
        return _orchestrator.ExecuteAsync(request, cancellationToken);
    }
}
