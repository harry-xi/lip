using Lip.Core.Context;
using Microsoft.Extensions.Logging;

namespace Lip.Core.Services;

public class UpdateService
{
    private readonly IContext _context;
    private readonly IPackageManager _packageManager;
    private readonly InstallService _installService;

    public UpdateService(IContext context)
    {
        _context = context;
        _installService = new InstallService(context);

        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);

        var pathManager = new PathManager(
            context.FileSystem,
            runtimeConfig.Cache,
            context.WorkingDir);

        var cacheManager = new CacheManager(
            context,
            pathManager,
            runtimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            runtimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

        _packageManager = new PackageManager(
            context.FileSystem,
            context.CommandRunner,
            context.Logger,
            context.UserInteraction,
            cacheManager,
            pathManager);
    }

    internal UpdateService(IContext context, IPackageManager packageManager, InstallService installService)
    {
        _context = context;
        _packageManager = packageManager;
        _installService = installService;
    }

    public async Task Update(
        List<string> userInputPackageTexts,
        bool dryRun = false,
        bool ignoreScripts = false,
        bool noDependencies = false)
    {
        List<string> packagesToUpdate = [];

        foreach (string packageText in userInputPackageTexts)
        {
            PackageIdentifier identifier = PackageIdentifier.Parse(packageText);
            PackageLock.Package? package = await _packageManager.GetPackageFromLock(identifier);

            if (package is null)
            {
                _context.Logger.LogWarning(
                    "Package '{identifier}' is not installed. Skipping.",
                    identifier);
                continue;
            }

            packagesToUpdate.Add(packageText);
        }

        if (packagesToUpdate.Count == 0)
        {
            return;
        }

        await _installService.Install(
            packagesToUpdate,
            dryRun: dryRun,
            ignoreScripts: ignoreScripts,
            noDependencies: noDependencies,
            upgradeLockedPackages: true,
            overwriteFiles: false);
    }
}