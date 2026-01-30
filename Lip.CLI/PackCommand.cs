using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("Create a package from the current directory.")]
class PackCommand : AsyncCommand<PackCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandOption("-o|--output <PATH>")]
        [Description("The output path for the created archive.")]
        public required string? OutputPath { get; init; }

        [CommandOption("--dry-run")]
        [Description("Do not actually create an archive.")]
        public required bool DryRun { get; init; }

        [CommandOption("--ignore-scripts")]
        [Description("Do not run any scripts during packing.")]
        public required bool IgnoreScripts { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var packService = new PackService(ctx);

        await packService.Pack(settings.OutputPath, new PackService.Args
        {
            DryRun = settings.DryRun,
            IgnoreScripts = settings.IgnoreScripts,
        });

        return 0;
    }
}