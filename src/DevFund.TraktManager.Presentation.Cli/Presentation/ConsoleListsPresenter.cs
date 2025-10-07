using System.Linq;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;
using Spectre.Console;

namespace DevFund.TraktManager.Presentation.Cli.Presentation;

public sealed class ConsoleListsPresenter : IListsPresenter
{
    private readonly IAnsiConsole _console;

    public ConsoleListsPresenter(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public Task PresentAsync(ListsResponse response, CancellationToken cancellationToken = default)
    {
        var rendered = false;

        if (response.SavedFilters.Count > 0)
        {
            RenderSavedFilters(response.SavedFilters, response.Pagination);
            rendered = true;
        }

        if (response.Lists.Count > 0)
        {
            RenderLists(response.Lists, response.Pagination);
            rendered = true;
        }

        if (response.ListItems.Count > 0)
        {
            foreach (var group in response.ListItems)
            {
                RenderListItems(group.List.List, group.Items);
            }

            rendered = true;
        }

        if (!rendered)
        {
            _console.MarkupLine("[yellow]No list data returned for the selected criteria.[/]");
        }

        return Task.CompletedTask;
    }

    private void RenderSavedFilters(IReadOnlyList<SavedFilter> filters, PaginationMetadata? pagination)
    {
        var table = new Table().Title("[green]Saved Filters[/]");
        table.AddColumn("Rank");
        table.AddColumn("Section");
        table.AddColumn("Name");
        table.AddColumn("Path");
        table.AddColumn("Query");
        table.AddColumn("Updated At");

        foreach (var filter in filters)
        {
            table.AddRow(
                filter.Rank.ToString(),
                filter.Section.ToString(),
                filter.Name,
                filter.Path,
                filter.Query,
                filter.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        }

        _console.Write(table);

        if (pagination is not null)
        {
            RenderPagination(pagination);
        }
    }

    private void RenderLists(IReadOnlyList<ListCollectionItem> lists, PaginationMetadata? pagination)
    {
        var table = new Table().Title("[green]Lists[/]");
        table.AddColumn("Name");
        table.AddColumn("Type");
        table.AddColumn("Privacy");
        table.AddColumn("Items");
        table.AddColumn("Likes");
        table.AddColumn("Comments");
        table.AddColumn("Owner");
        table.AddColumn("Updated");
        table.AddColumn("Origin");

        foreach (var entry in lists)
        {
            var list = entry.List;
            table.AddRow(
                list.Name,
                list.Type,
                list.Privacy.ToString(),
                list.ItemCount.ToString(),
                list.Likes.ToString(),
                list.CommentCount.ToString(),
                list.Owner?.Username ?? "—",
                list.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd"),
                FormatOrigin(entry));
        }

        _console.Write(table);

        if (pagination is not null)
        {
            RenderPagination(pagination);
        }
    }

    private void RenderListItems(UserList list, IReadOnlyList<ListItem> items)
    {
        var table = new Table().Title($"[blue]{Markup.Escape(list.Name)}[/]");
        table.AddColumn("Rank");
        table.AddColumn("Type");
        table.AddColumn("Item");
        table.AddColumn("Listed At");
        table.AddColumn("Notes");

        foreach (var item in items.OrderBy(i => i.Rank))
        {
            table.AddRow(
                item.Rank.ToString(),
                FormatItemType(item.ItemType),
                FormatItem(item),
                item.ListedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                string.IsNullOrWhiteSpace(item.Notes) ? string.Empty : item.Notes);
        }

        if (items.Count == 0)
        {
            _console.MarkupLine($"[yellow]No items returned for list '{Markup.Escape(list.Name)}'.[/]");
        }
        else
        {
            _console.Write(table);
        }
    }

    private void RenderPagination(PaginationMetadata metadata)
    {
        _console.MarkupLine($"[grey]Page {metadata.Page} of {metadata.PageCount} · Items {metadata.ItemCount} (Limit {metadata.Limit})[/]");
    }

    private static string FormatOrigin(ListCollectionItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Origin))
        {
            return string.Empty;
        }

        return item.OriginTimestamp is null
            ? item.Origin
            : $"{item.Origin} · {item.OriginTimestamp.Value.ToLocalTime():yyyy-MM-dd}";
    }

    private static string FormatItemType(ListItemType type) => type switch
    {
        ListItemType.Movie => "Movie",
        ListItemType.Show => "Show",
        ListItemType.Season => "Season",
        ListItemType.Episode => "Episode",
        ListItemType.Person => "Person",
        _ => "Unknown"
    };

    private static string FormatItem(ListItem item) => item.ItemType switch
    {
        ListItemType.Movie => item.Movie?.ToString() ?? "(missing movie)",
        ListItemType.Show => item.Show?.ToString() ?? "(missing show)",
        ListItemType.Season => item.Show is null || item.Season is null
            ? "(missing season)"
            : $"{item.Show} · Season {item.Season.Number}",
        ListItemType.Episode => item.Show is null || item.Episode is null
            ? "(missing episode)"
            : $"{item.Show} · {item.Episode}",
        ListItemType.Person => item.Person?.ToString() ?? "(missing person)",
        _ => "(unknown)"
    };
}