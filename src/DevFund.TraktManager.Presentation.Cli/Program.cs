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

using var scope = host.Services.CreateScope();
var serviceProvider = scope.ServiceProvider;
var console = serviceProvider.GetRequiredService<IAnsiConsole>();

try
{
    var (startDate, days) = ParseArguments(args);
    var request = new CalendarRequest(startDate, days);

    var tokenStore = serviceProvider.GetRequiredService<ITraktAccessTokenStore>();
    var existingToken = await tokenStore.GetTokenAsync(cts.Token);

    if (existingToken is null || string.IsNullOrWhiteSpace(existingToken.AccessToken))
    {
        var authService = serviceProvider.GetRequiredService<DeviceAuthenticationService>();
        console.MarkupLine("[yellow]Authorize this device with your Trakt account to continue.[/]");

        DeviceCodeResponse deviceCode;
        try
        {
            deviceCode = await authService.RequestDeviceCodeAsync(cts.Token);
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Failed to request device code: {Markup.Escape(ex.Message)}[/]");
            return;
        }

        console.MarkupLine($"Open [blue]{Markup.Escape(deviceCode.VerificationUri.ToString())}[/] in a browser.");
        console.MarkupLine($"Enter code: [bold]{Markup.Escape(deviceCode.UserCode)}[/]");
        console.MarkupLine($"Waiting for authorization (expires in {deviceCode.ExpiresIn.TotalMinutes:F1} minutes)...");

        try
        {
            await authService.WaitForAuthorizationAsync(deviceCode, cts.Token);
            console.MarkupLine("[green]Authorization successful.[/]");
        }
        catch (InvalidOperationException ex)
        {
            console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            return;
        }
        catch (TimeoutException ex)
        {
            console.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            return;
        }
    }

    var orchestrator = serviceProvider.GetRequiredService<CalendarOrchestrator>();

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
