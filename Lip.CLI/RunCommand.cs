using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("Run a script from the current tooth.")]
class RunCommand : AsyncCommand<RunCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandArgument(0, "<script>")]
        [Description("The script to run.")]
        public required string Script { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var prep = await CommandRoot.Prepare(settings);

        var runService = new RunService(prep.Context, prep.PackageManager, prep.PathManager);

        await runService.Run(settings.Script, new()
        {
            VariantLabel = string.Empty,
        });

        return 0;
    }
}