using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

class CacheSettings : BaseCommandSettings { }

[Description("Clean the cache.")]
class CacheCleanCommand : AsyncCommand<CacheCleanCommand.Settings>
{
    public class Settings : CacheSettings { }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var cacheService = new CacheService(ctx);

        await cacheService.Clean();

        return 0;
    }
}
