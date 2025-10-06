using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.DependencyInjection;
using DevFund.TraktManager.Application.Services;
using DevFund.TraktManager.Infrastructure.DependencyInjection;
using DevFund.TraktManager.Presentation.Cli.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApplicationLayer()
    .AddInfrastructureLayer(builder.Configuration)
    .AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console)
    .AddSingleton<ICalendarPresenter, ConsoleCalendarPresenter>();

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

try
{
    var (startDate, days) = ParseArguments(args);
    var request = new CalendarRequest(startDate, days);

    using var scope = host.Services.CreateScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<CalendarOrchestrator>();

    await orchestrator.ExecuteAsync(request, cts.Token);
}
catch (OperationCanceledException)
{
    // Graceful shutdown.
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}

await host.StopAsync();

static (DateOnly startDate, int days) ParseArguments(string[] arguments)
{
    DateOnly? startDate = null;
    int? days = null;

    foreach (var argument in arguments)
    {
        if (argument.StartsWith("--start="))
        {
            var value = argument.Substring("--start=".Length);
            if (!DateOnly.TryParse(value, out var parsed))
            {
                throw new ArgumentException($"Invalid --start value '{value}'. Use yyyy-MM-dd format.");
            }

            startDate = parsed;
        }
        else if (argument.StartsWith("--days="))
        {
            var value = argument.Substring("--days=".Length);
            if (!int.TryParse(value, out var parsed))
            {
                throw new ArgumentException($"Invalid --days value '{value}'.");
            }

            days = parsed;
        }
    }

    startDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
    days ??= 7;

    return (startDate.Value, days.Value);
}
