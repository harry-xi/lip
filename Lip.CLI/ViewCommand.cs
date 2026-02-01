using Lip.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("View package information.")]
class ViewCommand : AsyncCommand<ViewCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandArgument(0, "<package>")]
        [Description("The package specifier to view.")]
        public required string Package { get; init; }

        [CommandArgument(1, "[path]")]
        [Description("The path to a specific field in the manifest.")]
        public required string? Path { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var viewService = new ViewService(ctx);

        string result = await viewService.View(settings.Package, settings.Path);

        AnsiConsole.MarkupLine(result.EscapeMarkup());

        return 0;
    }
}