using System.IO;
using System.Linq;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.DependencyInjection;
using DevFund.TraktManager.Domain.ValueObjects;
using DevFund.TraktManager.Infrastructure.DependencyInjection;
using DevFund.TraktManager.Presentation.Cli;
using DevFund.TraktManager.Presentation.Cli.Presentation;
using DevFund.TraktManager.Presentation.Cli.Presentation.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

CliOptions cliOptions;
try
{
    cliOptions = ParseCliOptions(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return;
}

var builder = Host.CreateApplicationBuilder(args);

var configurationBasePath = AppContext.BaseDirectory;
builder.Configuration
    .AddJsonFile(Path.Combine(configurationBasePath, "appsettings.json"), optional: true, reloadOnChange: true)
    .AddJsonFile(Path.Combine(configurationBasePath, "appsettings.Local.json"), optional: true, reloadOnChange: true);

builder.Services
    .AddApplicationLayer()
    .AddInfrastructureLayer(builder.Configuration)
    .AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console)
    .AddSingleton<ICalendarPresenter, ConsoleCalendarPresenter>()
    .AddSingleton<IWatchlistPresenter, ConsoleWatchlistPresenter>()
    .AddSingleton<IListsPresenter, ConsoleListsPresenter>()
    .AddScoped<ICliCommandStrategy, CalendarCliCommandStrategy>()
    .AddScoped<ICliCommandStrategy, WatchlistCliCommandStrategy>()
    .AddScoped<ICliCommandStrategy, ListsCliCommandStrategy>();

using var host = builder.Build();
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    if (!cts.IsCancellationRequested)
    {
        cts.Cancel();
    }
};

await host.StartAsync(cts.Token);

using var scope = host.Services.CreateScope();
var serviceProvider = scope.ServiceProvider;
var console = serviceProvider.GetRequiredService<IAnsiConsole>();

try
{
    if (!await EnsureAuthenticatedAsync(serviceProvider, console, cts.Token).ConfigureAwait(false))
    {
        await host.StopAsync(cts.Token).ConfigureAwait(false);
        return;
    }

    var strategies = serviceProvider.GetRequiredService<IEnumerable<ICliCommandStrategy>>();
    var strategy = strategies.FirstOrDefault(s => s.CanHandle(cliOptions));

    if (strategy is null)
    {
        console.MarkupLine("[red]No CLI strategy is registered for the requested mode.[/]");
        await host.StopAsync(cts.Token).ConfigureAwait(false);
        return;
    }

    await strategy.ExecuteAsync(cliOptions, cts.Token).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    // Graceful shutdown.
}
catch (Exception ex)
{
    console.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
}

await host.StopAsync(cts.Token).ConfigureAwait(false);

static CliOptions ParseCliOptions(string[] arguments)
{
    var mode = CliMode.Calendar;
    DateOnly? startDate = null;
    int? days = null;
    var watchlistFilter = WatchlistItemFilter.All;
    var watchlistSort = WatchlistSortField.Rank;
    var watchlistOrder = WatchlistSortOrder.Asc;
    var listsKind = ListCollectionKind.Personal;
    string? listsUser = null;
    string? listsSlug = null;
    var listsItemType = ListItemsType.All;
    var listsIncludeItems = false;
    int? listsPage = null;
    int? listsLimit = null;
    var listsSection = SavedFilterSection.Movies;

    foreach (var argument in arguments)
    {
        if (argument.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--mode=".Length).Trim().ToLowerInvariant();
            mode = value switch
            {
                "calendar" => CliMode.Calendar,
                "watchlist" => CliMode.Watchlist,
                "lists" => CliMode.Lists,
                _ => throw new ArgumentException($"Unsupported mode '{value}'. Use 'calendar', 'watchlist', or 'lists'.")
            };
        }
        else if (argument.StartsWith("--start=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--start=".Length);
            if (!DateOnly.TryParse(value, out var parsed))
            {
                throw new ArgumentException($"Invalid --start value '{value}'. Use yyyy-MM-dd format.");
            }

            startDate = parsed;
        }
        else if (argument.StartsWith("--days=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--days=".Length);
            if (!int.TryParse(value, out var parsed) || parsed <= 0)
            {
                throw new ArgumentException($"Invalid --days value '{value}'. Provide a positive integer.");
            }

            days = parsed;
        }
        else if (argument.StartsWith("--watchlist-type=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--watchlist-type=".Length);
            watchlistFilter = ParseWatchlistItemFilter(value);
        }
        else if (argument.StartsWith("--watchlist-sort=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--watchlist-sort=".Length);
            watchlistSort = ParseWatchlistSortField(value);
        }
        else if (argument.StartsWith("--watchlist-order=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--watchlist-order=".Length);
            watchlistOrder = ParseWatchlistSortOrder(value);
        }
        else if (argument.StartsWith("--lists-kind=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--lists-kind=".Length);
            listsKind = ParseListCollectionKind(value);
        }
        else if (argument.StartsWith("--lists-user=", StringComparison.OrdinalIgnoreCase))
        {
            listsUser = argument.Substring("--lists-user=".Length);
        }
        else if (argument.StartsWith("--lists-slug=", StringComparison.OrdinalIgnoreCase))
        {
            listsSlug = argument.Substring("--lists-slug=".Length);
        }
        else if (argument.StartsWith("--lists-item-type=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--lists-item-type=".Length);
            listsItemType = ParseListItemsType(value);
        }
        else if (argument.StartsWith("--lists-include-items", StringComparison.OrdinalIgnoreCase))
        {
            var parts = argument.Split('=', 2);
            if (parts.Length == 1)
            {
                listsIncludeItems = true;
            }
            else if (bool.TryParse(parts[1], out var parsedInclude))
            {
                listsIncludeItems = parsedInclude;
            }
            else
            {
                throw new ArgumentException($"Invalid --lists-include-items value '{parts[1]}'. Use true or false.");
            }
        }
        else if (argument.StartsWith("--lists-page=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--lists-page=".Length);
            if (!int.TryParse(value, out var parsed) || parsed <= 0)
            {
                throw new ArgumentException($"Invalid --lists-page value '{value}'. Provide a positive integer.");
            }

            listsPage = parsed;
        }
        else if (argument.StartsWith("--lists-limit=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--lists-limit=".Length);
            if (!int.TryParse(value, out var parsed) || parsed <= 0)
            {
                throw new ArgumentException($"Invalid --lists-limit value '{value}'. Provide a positive integer.");
            }

            listsLimit = parsed;
        }
        else if (argument.StartsWith("--lists-section=", StringComparison.OrdinalIgnoreCase))
        {
            var value = argument.Substring("--lists-section=".Length);
            listsSection = ParseSavedFilterSection(value);
        }
    }

    var calendarOptions = new CalendarOptions(startDate ?? DateOnly.FromDateTime(DateTime.UtcNow), days ?? 7);
    var watchlistOptions = new WatchlistOptions(watchlistFilter, watchlistSort, watchlistOrder);
    var listsOptions = new ListsOptions(listsKind, listsUser, listsSlug, listsItemType, listsIncludeItems, listsPage, listsLimit, listsSection);
    return new CliOptions(mode, calendarOptions, watchlistOptions, listsOptions);
}

static WatchlistItemFilter ParseWatchlistItemFilter(string value)
{
    return value.Trim().ToLowerInvariant() switch
    {
        "all" => WatchlistItemFilter.All,
        "movies" => WatchlistItemFilter.Movies,
        "shows" => WatchlistItemFilter.Shows,
        "seasons" => WatchlistItemFilter.Seasons,
        "episodes" => WatchlistItemFilter.Episodes,
        _ => throw new ArgumentException($"Unsupported watchlist type '{value}'.")
    };
}

static WatchlistSortField ParseWatchlistSortField(string value)
{
    var normalized = value.Trim().ToLowerInvariant().Replace('-', '_');
    return normalized switch
    {
        "rank" => WatchlistSortField.Rank,
        "added" => WatchlistSortField.Added,
        "title" => WatchlistSortField.Title,
        "released" => WatchlistSortField.Released,
        "runtime" => WatchlistSortField.Runtime,
        "popularity" => WatchlistSortField.Popularity,
        "random" => WatchlistSortField.Random,
        "percentage" => WatchlistSortField.Percentage,
        "imdb_rating" => WatchlistSortField.ImdbRating,
        "tmdb_rating" => WatchlistSortField.TmdbRating,
        "rt_tomatometer" => WatchlistSortField.RtTomatometer,
        "rt_audience" => WatchlistSortField.RtAudience,
        "metascore" => WatchlistSortField.Metascore,
        "votes" => WatchlistSortField.Votes,
        "imdb_votes" => WatchlistSortField.ImdbVotes,
        "tmdb_votes" => WatchlistSortField.TmdbVotes,
        "my_rating" => WatchlistSortField.MyRating,
        "watched" => WatchlistSortField.Watched,
        "collected" => WatchlistSortField.Collected,
        _ => throw new ArgumentException($"Unsupported watchlist sort '{value}'.")
    };
}

static WatchlistSortOrder ParseWatchlistSortOrder(string value)
{
    return value.Trim().ToLowerInvariant() switch
    {
        "asc" => WatchlistSortOrder.Asc,
        "desc" => WatchlistSortOrder.Desc,
        _ => throw new ArgumentException($"Unsupported watchlist order '{value}'.")
    };
}

static ListCollectionKind ParseListCollectionKind(string value)
{
    return value.Trim().ToLowerInvariant() switch
    {
        "personal" => ListCollectionKind.Personal,
        "liked" => ListCollectionKind.Liked,
        "likes" => ListCollectionKind.Likes,
        "official" => ListCollectionKind.Official,
        "saved" => ListCollectionKind.Saved,
        _ => throw new ArgumentException($"Unsupported lists kind '{value}'.")
    };
}

static ListItemsType ParseListItemsType(string value)
{
    return value.Trim().ToLowerInvariant() switch
    {
        "all" => ListItemsType.All,
        "movies" or "movie" => ListItemsType.Movies,
        "shows" or "show" => ListItemsType.Shows,
        "seasons" or "season" => ListItemsType.Seasons,
        "episodes" or "episode" => ListItemsType.Episodes,
        "people" or "person" => ListItemsType.People,
        _ => throw new ArgumentException($"Unsupported lists item type '{value}'.")
    };
}

static SavedFilterSection ParseSavedFilterSection(string value)
{
    return value.Trim().ToLowerInvariant() switch
    {
        "movies" => SavedFilterSection.Movies,
        "shows" => SavedFilterSection.Shows,
        "calendars" => SavedFilterSection.Calendars,
        "search" => SavedFilterSection.Search,
        _ => throw new ArgumentException($"Unsupported saved filter section '{value}'.")
    };
}

static async Task<bool> EnsureAuthenticatedAsync(IServiceProvider serviceProvider, IAnsiConsole console, CancellationToken cancellationToken)
{
    var tokenStore = serviceProvider.GetRequiredService<ITraktAccessTokenStore>();
    var existingToken = await tokenStore.GetTokenAsync(cancellationToken).ConfigureAwait(false);

    if (existingToken is not null && !string.IsNullOrWhiteSpace(existingToken.AccessToken))
    {
        return true;
    }

    var authService = serviceProvider.GetRequiredService<IDeviceAuthenticationService>();
    console.MarkupLine("[yellow]Authorize this device with your Trakt account to continue.[/]");

    DeviceCodeResponse deviceCode;
    try
    {
        deviceCode = await authService.RequestDeviceCodeAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        console.MarkupLine($"[red]Failed to request device code: {Markup.Escape(ex.Message)}[/]");
        return false;
    }

    console.MarkupLine($"Open [blue]{Markup.Escape(deviceCode.VerificationUri.ToString())}[/] in a browser.");
    console.MarkupLine($"Enter code: [bold]{Markup.Escape(deviceCode.UserCode)}[/]");
    console.MarkupLine($"Waiting for authorization (expires in {deviceCode.ExpiresIn.TotalMinutes:F1} minutes)...");

    try
    {
        await authService.WaitForAuthorizationAsync(deviceCode, cancellationToken).ConfigureAwait(false);
        console.MarkupLine("[green]Authorization successful.[/]");
        return true;
    }
    catch (InvalidOperationException ex)
    {
        console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
        return false;
    }
    catch (TimeoutException ex)
    {
        console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
        return false;
    }
}
