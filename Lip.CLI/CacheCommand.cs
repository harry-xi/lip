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
        var ctx = await CommandRoot.CreateContext(settings);

        var cacheService = new CacheService(ctx);

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
        var ctx = await CommandRoot.CreateContext(settings);

        var cacheService = new CacheService(ctx);

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
        var ctx = await CommandRoot.CreateContext(settings);

        var cacheService = new CacheService(ctx);

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