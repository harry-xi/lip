using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

public class UpdateCommand(ILipClient lipClient) : AsyncCommand<UpdateCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<PACKAGES>")]
        [Description("The packages to update")]
        public required string[] Packages { get; init; }

        [CommandOption("--dry-run")]
        [Description("Run without making any changes")]
        public bool DryRun { get; init; }

        [CommandOption("--ignore-scripts")]
        [Description("Skip running update scripts")]
        public bool IgnoreScripts { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await _lipClient.Update(settings.Packages, settings.DryRun, settings.IgnoreScripts);
        AnsiConsole.MarkupLine("[green]Packages updated successfully.[/]");
        return 0;
    }
}