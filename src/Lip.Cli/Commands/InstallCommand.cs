using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

public class InstallCommand(ILipClient lipClient) : AsyncCommand<InstallCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<PACKAGES>")]
        [Description("The packages to install")]
        public required string[] Packages { get; init; }

        [CommandOption("--dry-run")]
        [Description("Perform a dry run without making changes")]
        public bool DryRun { get; init; }

        [CommandOption("--ignore-scripts")]
        [Description("Ignore scripts during installation")]
        public bool IgnoreScripts { get; init; }

        [CommandOption("--no-dependencies")]
        [Description("Do not install dependencies")]
        public bool NoDependencies { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await _lipClient.Install(settings.Packages, settings.DryRun, settings.IgnoreScripts, settings.NoDependencies);
        AnsiConsole.MarkupLine("[green]Packages installed successfully.[/]");
        return 0;
    }
}