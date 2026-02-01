using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("Uninstall packages.")]
class UninstallCommand : AsyncCommand<UninstallCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandArgument(0, "<package ...>")]
        [Description("The packages to uninstall.")]
        public required string[] Packages { get; init; }

        [CommandOption("--dry-run")]
        [Description("Do not actually uninstall any packages.")]
        public required bool DryRun { get; init; }

        [CommandOption("--ignore-scripts")]
        [Description("Do not run any scripts during uninstallation.")]
        public required bool IgnoreScripts { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var uninstallService = new UninstallService(ctx);

        await uninstallService.Uninstall(
            [.. settings.Packages],
            dryRun: settings.DryRun,
            ignoreScripts: settings.IgnoreScripts);

        return 0;
    }
}