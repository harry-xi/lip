using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("Migrate a tooth.json file to the latest format.")]
class MigrateCommand : AsyncCommand<MigrateCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandArgument(0, "[path]")]
        [Description("The path to the tooth.json file to migrate.")]
        public required string Path { get; init; } = "tooth.json";

        [CommandOption("-o|--output <PATH>")]
        [Description("The output path for the migrated file.")]
        public required string? Output { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var prep = await CommandRoot.Prepare(settings);

        var migrateService = new MigrateService(prep.Context, prep.PathManager);

        await migrateService.Migrate(settings.Path, settings.Output, new());

        return 0;
    }
}