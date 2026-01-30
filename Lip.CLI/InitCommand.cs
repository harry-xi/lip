using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("Initialize a new tooth in the current directory.")]
class InitCommand : AsyncCommand<InitCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandOption("-f|--force")]
        [Description("Overwrite the existing tooth.json file.")]
        public required bool Force { get; init; }

        [CommandOption("-y|--yes")]
        [Description("Use default values for all prompts.")]
        public required bool Yes { get; init; }

        [CommandOption("--init-avatar-url <URL>")]
        [Description("The avatar URL for the package.")]
        public required string? InitAvatarUrl { get; init; }

        [CommandOption("--init-description <DESCRIPTION>")]
        [Description("The description for the package.")]
        public required string? InitDescription { get; init; }

        [CommandOption("--init-name <NAME>")]
        [Description("The name for the package.")]
        public required string? InitName { get; init; }

        [CommandOption("--init-tooth <TOOTH>")]
        [Description("The tooth path for the package.")]
        public required string? InitTooth { get; init; }

        [CommandOption("--init-version <VERSION>")]
        [Description("The version for the package.")]
        public required string? InitVersion { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var initService = new InitService(ctx);

        await initService.Init(new InitService.Args
        {
            Force = settings.Force,
            Yes = settings.Yes,
            InitAvatarUrl = settings.InitAvatarUrl,
            InitDescription = settings.InitDescription,
            InitName = settings.InitName,
            InitTooth = settings.InitTooth,
            InitVersion = settings.InitVersion,
        });

        return 0;
    }
}