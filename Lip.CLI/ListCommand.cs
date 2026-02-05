using Lip.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("List installed packages.")]
class ListCommand : AsyncCommand<ListCommand.Settings>
{
    public class Settings : BaseCommandSettings { }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var listService = new ListService(ctx);

        var result = await listService.List();

        foreach (var specifier in result)
        {
            AnsiConsole.MarkupLine($"{specifier}".EscapeMarkup());
        }

        return 0;
    }
}