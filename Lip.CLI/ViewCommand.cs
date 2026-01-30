using Lip.Core.PackageRegistries;
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

        [CommandOption("-p|--path <PATH>")]
        [Description("The path to a specific field in the manifest.")]
        public required string? Path { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var prep = await CommandRoot.Prepare(settings);

        var packageRegistry = new PackageRegistry(
            prep.Context,
            prep.CacheManager,
            prep.PathManager,
            prep.RuntimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            prep.RuntimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

        var viewService = new ViewService(packageRegistry);

        string result = await viewService.View(settings.Package, settings.Path, new());

        AnsiConsole.MarkupLine(result.EscapeMarkup());

        return 0;
    }
}