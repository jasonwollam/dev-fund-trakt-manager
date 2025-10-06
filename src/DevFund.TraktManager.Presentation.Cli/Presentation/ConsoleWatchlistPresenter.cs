using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;
using Spectre.Console;
using System.Linq;

namespace DevFund.TraktManager.Presentation.Cli.Presentation;

public sealed class ConsoleWatchlistPresenter : IWatchlistPresenter
{
    private readonly IAnsiConsole _console;

    public ConsoleWatchlistPresenter(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public Task PresentAsync(IReadOnlyList<WatchlistEntry> entries, CancellationToken cancellationToken = default)
    {
        var table = new Table().Title("[green]Watchlist[/]");
        table.AddColumn("Rank");
        table.AddColumn("Type");
        table.AddColumn("Item");
        table.AddColumn("Listed At");
        table.AddColumn("Notes");

        foreach (var entry in entries.OrderBy(e => e.Rank))
        {
            table.AddRow(
                entry.Rank.ToString(),
                FormatType(entry.ItemType),
                FormatItem(entry),
                entry.ListedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                string.IsNullOrWhiteSpace(entry.Notes) ? "" : entry.Notes);
        }

        if (entries.Count == 0)
        {
            _console.MarkupLine("[yellow]No watchlist items returned for the selected criteria.[/]");
        }
        else
        {
            _console.Write(table);
        }

        return Task.CompletedTask;
    }

    private static string FormatType(WatchlistItemType itemType) => itemType switch
    {
        WatchlistItemType.Movie => "Movie",
        WatchlistItemType.Show => "Show",
        WatchlistItemType.Season => "Season",
        WatchlistItemType.Episode => "Episode",
        _ => "Unknown"
    };

    private static string FormatItem(WatchlistEntry entry) => entry.ItemType switch
    {
        WatchlistItemType.Movie => entry.Movie?.ToString() ?? "(missing movie)",
        WatchlistItemType.Show => entry.Show?.ToString() ?? "(missing show)",
        WatchlistItemType.Season => entry.Show is null || entry.Season is null
            ? "(missing season)"
            : $"{entry.Show} · Season {entry.Season.Number}",
        WatchlistItemType.Episode => entry.Show is null || entry.Episode is null
            ? "(missing episode)"
            : $"{entry.Show} · {entry.Episode}",
        _ => "(unknown)"
    };
}
