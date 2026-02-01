using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("Update installed packages to new versions.")]
class UpdateCommand : AsyncCommand<UpdateCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandArgument(0, "<package ...>")]
        [Description("The packages to update.")]
        public required string[] Packages { get; init; }

        [CommandOption("--dry-run")]
        [Description("Do not actually update any packages.")]
        public required bool DryRun { get; init; }

        [CommandOption("--ignore-scripts")]
        [Description("Do not run any scripts during update.")]
        public required bool IgnoreScripts { get; init; }

        [CommandOption("--no-dependencies")]
        [Description("Bypass dependency resolution and only update the specified packages.")]
        public required bool NoDependencies { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var updateService = new UpdateService(ctx);

        await updateService.Update(
            [.. settings.Packages],
            dryRun: settings.DryRun,
            ignoreScripts: settings.IgnoreScripts,
            noDependencies: settings.NoDependencies);

        return 0;
    }
}