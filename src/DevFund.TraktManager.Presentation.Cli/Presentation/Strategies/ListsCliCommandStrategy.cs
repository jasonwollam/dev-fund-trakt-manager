using System;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.Services;

namespace DevFund.TraktManager.Presentation.Cli.Presentation.Strategies;

public sealed class ListsCliCommandStrategy : ICliCommandStrategy
{
    private readonly ListsOrchestrator _orchestrator;

    public ListsCliCommandStrategy(ListsOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public bool CanHandle(CliOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Mode == CliMode.Lists;
    }

    public Task ExecuteAsync(CliOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        var listOptions = options.Lists;
        var request = new ListsRequest(
            listOptions.Kind,
            listOptions.UserSlug,
            listOptions.ListSlug,
            listOptions.ItemType,
            listOptions.IncludeItems,
            listOptions.Page,
            listOptions.Limit,
            listOptions.Section);

        return _orchestrator.ExecuteAsync(request, cancellationToken);
    }
}