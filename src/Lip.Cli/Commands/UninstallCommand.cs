using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

public class UninstallCommand(ILipClient lipClient) : AsyncCommand<UninstallCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<PACKAGES>")]
        [Description("The packages to uninstall")]
        public required string[] Packages { get; init; }

        [CommandOption("--dry-run")]
        [Description("Run without making any changes")]
        public bool DryRun { get; init; }

        [CommandOption("--ignore-scripts")]
        [Description("Skip running uninstall scripts")]
        public bool IgnoreScripts { get; init; }

        [CommandOption("--no-dependencies")]
        [Description("Skip removing dependencies")]
        public bool NoDependencies { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await _lipClient.Uninstall(settings.Packages, settings.DryRun, settings.IgnoreScripts, settings.NoDependencies);
        AnsiConsole.MarkupLine("[green]Packages uninstalled successfully.[/]");
        return 0;
    }
}