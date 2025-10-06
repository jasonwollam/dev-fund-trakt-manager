using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;
using DevFund.TraktManager.Presentation.Cli.Presentation;
using Spectre.Console.Testing;

namespace DevFund.TraktManager.Presentation.Cli.Tests;

public class ConsoleCalendarPresenterTests
{
    [Fact]
    public async Task PresentAsync_WritesTable_WhenEntriesExist()
    {
        var console = new TestConsole();
        var presenter = new ConsoleCalendarPresenter(console);
        var entries = new[]
        {
            new CalendarEntry(new DateOnly(2024, 01, 01), new Show("Test", 2024, new TraktIds(1, "show")), new Episode(1, 1, "Pilot", new TraktIds(2, "episode")))
        };

        await presenter.PresentAsync(entries);

        Assert.Contains("Upcoming Shows", console.Output);
        Assert.Contains("S01E01", console.Output);
    }

    [Fact]
    public async Task PresentAsync_WritesEmptyMessage_WhenNoEntries()
    {
        var console = new TestConsole();
        var presenter = new ConsoleCalendarPresenter(console);

        await presenter.PresentAsync(Array.Empty<CalendarEntry>());

        Assert.Contains("No upcoming episodes", console.Output);
    }
}