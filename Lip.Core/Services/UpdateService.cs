using Lip.Core.Context;

using Microsoft.Extensions.Logging;

namespace Lip.Core.Services;

public class UpdateService
{
    private readonly IContext _context;
    private readonly IWorkspaceManager _workspaceManager;
    private readonly InstallService _installService;

    public UpdateService(IContext context)
    {
        _context = context;

        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);
        var pathManager = ServiceFactory.CreatePathManager(context, runtimeConfig);
        var cacheManager = ServiceFactory.CreateCacheManager(context, pathManager, runtimeConfig);
        var registry = ServiceFactory.CreatePackageRegistry(context, pathManager, cacheManager, runtimeConfig);
        var solver = new DependencySolver(context.Logger, registry);

        _workspaceManager = ServiceFactory.CreateWorkspaceManager(context, pathManager, cacheManager);

        _installService = new InstallService(
            context,
            _workspaceManager,
            solver,
            cacheManager,
            registry,
            pathManager);
    }

    internal UpdateService(IContext context, IWorkspaceManager workspaceManager, InstallService installService)
    {
        _context = context;
        _workspaceManager = workspaceManager;
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
            PackageLock.Package? package = await _workspaceManager.GetPackageFromLock(identifier);

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