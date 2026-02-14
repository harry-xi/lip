using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

[Description("Shows available versions for a package")]
public class VersionsCommand(ILipClient lipClient) : AsyncCommand<VersionsCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<PACKAGE>")]
        [Description("The package to show versions for")]
        public required string Package { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        IEnumerable<string> versions = await _lipClient.Versions(settings.Package);
        foreach (string version in versions)
        {
            AnsiConsole.WriteLine(version);
        }
        return 0;
    }
}