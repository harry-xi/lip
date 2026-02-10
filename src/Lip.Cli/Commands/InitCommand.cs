using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Lip.Cli.Commands;

public class InitCommand(ILipClient lipClient) : AsyncCommand<InitCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await _lipClient.Init();
        AnsiConsole.MarkupLine("[green]Initialized Lip project.[/]");
        return 0;
    }
}