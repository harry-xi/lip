using Lip.Core;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("Update installed packages to new versions.")]
class UpdateCommand : AsyncCommand<UpdateCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandArgument(0, "<package ...>")]
        [Description("The packages to update.")]
        public required string[] Packages { get; init; }

        [CommandOption("--dry-run")]
        [Description("Do not actually update any packages.")]
        public required bool DryRun { get; init; }

        [CommandOption("--ignore-scripts")]
        [Description("Do not run any scripts during update.")]
        public required bool IgnoreScripts { get; init; }

        [CommandOption("--no-dependencies")]
        [Description("Bypass dependency resolution and only update the specified packages.")]
        public required bool NoDependencies { get; init; }
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

        var dependencySolver = new DependencySolver(prep.Context, packageRegistry);

        var installService = new InstallService(
            prep.Context,
            prep.PackageManager,
            dependencySolver,
            prep.CacheManager,
            packageRegistry,
            prep.PathManager);

        var updateService = new UpdateService(prep.Context, prep.PackageManager, installService);

        await updateService.Update([.. settings.Packages], new()
        {
            DryRun = settings.DryRun,
            IgnoreScripts = settings.IgnoreScripts,
            NoDependencies = settings.NoDependencies,
        });

        return 0;
    }
}