namespace DevFund.TraktManager.Presentation.Cli.Presentation.Strategies;

public interface ICliCommandStrategy
{
    bool CanHandle(CliOptions options);

    Task ExecuteAsync(CliOptions options, CancellationToken cancellationToken);
}
