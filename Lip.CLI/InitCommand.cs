using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("Initialize a new tooth in the current directory.")]
class InitCommand : AsyncCommand<InitCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var initService = new InitService(ctx);

        await initService.Init();

        return 0;
    }
}