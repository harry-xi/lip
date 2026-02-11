using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

[Description("Shows version information")]
public class VersionCommand(ILipClient lipClient) : AsyncCommand<VersionCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        string version = await _lipClient.Version();

        AnsiConsole.WriteLine(version);

        return 0;
    }
}