using Lip.Core;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("Install packages and their dependencies from various sources.")]
class InstallCommand : AsyncCommand<InstallCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandArgument(0, "[package ...]")]
        [Description("The packages to install. If no packages are specified, lip will install the package in the current directory.")]
        public required string[]? Packages { get; init; }

        [CommandOption("--dry-run")]
        [Description("Do not actually install any packages. Be aware that files will still be downloaded and cached.")]
        public required bool DryRun { get; init; }

        [CommandOption("--ignore-scripts")]
        [Description("Do not run any scripts during installation.")]
        public required bool IgnoreScripts { get; init; }

        [CommandOption("--no-dependencies")]
        [Description("Bypass dependency resolution and only install the specified packages.")]
        public required bool NoDependencies { get; init; }

        [CommandOption("--overwrite-files")]
        [Description("Overwrite existing files in the folder")]
        public required bool OverwriteFiles { get; init; }
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

        await installService.Install(settings.Packages?.ToList(), new InstallService.Args
        {
            DryRun = settings.DryRun,
            IgnoreScripts = settings.IgnoreScripts,
            NoDependencies = settings.NoDependencies,
            UpgradeLockedPackages = false,
            OverwriteFiles = settings.OverwriteFiles,
        });

        return 0;
    }
}