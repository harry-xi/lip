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

        List<ListService.ResultItem> result = await listService.List(new());

        foreach (ListService.ResultItem item in result)
        {
            AnsiConsole.MarkupLine($"{item.Specifier}".EscapeMarkup());
        }

        return 0;
    }
}