using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Lip.Cli.Commands;

public class CacheCleanCommand(ILipClient lipClient) : AsyncCommand<CacheCleanCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await _lipClient.CacheClean();
        AnsiConsole.MarkupLine("[green]Cache cleaned successfully.[/]");
        return 0;
    }
}