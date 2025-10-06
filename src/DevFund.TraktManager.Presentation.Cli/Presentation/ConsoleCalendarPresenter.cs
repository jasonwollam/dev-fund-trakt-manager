using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Domain.Entities;
using Spectre.Console;
using System.Linq;

namespace DevFund.TraktManager.Presentation.Cli.Presentation;

public sealed class ConsoleCalendarPresenter : ICalendarPresenter
{
    private readonly IAnsiConsole _console;

    public ConsoleCalendarPresenter(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public Task PresentAsync(IReadOnlyList<CalendarEntry> entries, CancellationToken cancellationToken = default)
    {
        var table = new Table().Title("[green]Upcoming Shows[/]");
        table.AddColumn("Date");
        table.AddColumn("Show");
        table.AddColumn("Episode");

        foreach (var entry in entries.OrderBy(e => e.FirstAired))
        {
            table.AddRow(
                entry.FirstAired.ToString("yyyy-MM-dd"),
                entry.Show.ToString(),
                entry.Episode.ToString());
        }

        if (entries.Count == 0)
        {
            _console.MarkupLine("[yellow]No upcoming episodes found for the selected window.[/]");
        }
        else
        {
            _console.Write(table);
        }

        return Task.CompletedTask;
    }
}
