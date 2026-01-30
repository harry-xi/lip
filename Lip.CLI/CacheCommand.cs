using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

class CacheSettings : BaseCommandSettings { }

[Description("Add a package to the cache.")]
class CacheAddCommand : AsyncCommand<CacheAddCommand.Settings>
{
    public class Settings : CacheSettings
    {
        [CommandArgument(0, "<package>")]
        [Description("The package specifier to add.")]
        public required string Package { get; init; }
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

        var cacheService = new CacheService(packageRegistry, prep.CacheManager);

        await cacheService.Add(settings.Package, new());

        return 0;
    }
}

[Description("Clean the cache.")]
class CacheCleanCommand : AsyncCommand<CacheCleanCommand.Settings>
{
    public class Settings : CacheSettings { }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var prep = await CommandRoot.Prepare(settings);

        var packageRegistry = new PackageRegistry(
            prep.Context,
            prep.CacheManager,
            prep.PathManager,
            prep.RuntimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            prep.RuntimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

        var cacheService = new CacheService(packageRegistry, prep.CacheManager);

        await cacheService.Clean(new());

        return 0;
    }
}

[Description("List the cache contents.")]
class CacheListCommand : AsyncCommand<CacheListCommand.Settings>
{
    public class Settings : CacheSettings { }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var prep = await CommandRoot.Prepare(settings);

        var packageRegistry = new PackageRegistry(
            prep.Context,
            prep.CacheManager,
            prep.PathManager,
            prep.RuntimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            prep.RuntimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

        var cacheService = new CacheService(packageRegistry, prep.CacheManager);

        CacheService.ListResult result = await cacheService.List(new());

        AnsiConsole.MarkupLine("[bold]Downloaded Files:[/]");
        foreach (string file in result.DownloadedFiles)
        {
            AnsiConsole.MarkupLine($"  {file}".EscapeMarkup());
        }

        AnsiConsole.MarkupLine("[bold]Git Repos:[/]");
        foreach (string repo in result.GitRepos)
        {
            AnsiConsole.MarkupLine($"  {repo}".EscapeMarkup());
        }

        return 0;
    }
}