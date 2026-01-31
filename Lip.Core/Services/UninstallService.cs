using Lip.Core.Context;
using Microsoft.Extensions.Logging;

namespace Lip.Core.Services;

public class UninstallService
{
    private readonly IContext _context;
    private readonly IPackageManager _packageManager;

    public UninstallService(IContext context)
    {
        _context = context;

        var pathManager = new PathManager(
            context.FileSystem,
            context.RuntimeConfig.Cache,
            context.WorkingDir);

        var cacheManager = new CacheManager(
            context,
            pathManager,
            context.RuntimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            context.RuntimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

        _packageManager = new PackageManager(context, cacheManager, pathManager);
    }

    internal UninstallService(IContext context, IPackageManager packageManager)
    {
        _context = context;
        _packageManager = packageManager;
    }

    public async Task Uninstall(
        List<string> packageSpecifierTextsToUninstall,
        bool dryRun = false,
        bool ignoreScripts = false)
    {
        List<PackageIdentifier> packageSpecifiersToUninstallSpecified =
            packageSpecifierTextsToUninstall.ConvertAll(PackageIdentifier.Parse);

        // Remove non-installed packages and sort packages topologically.

        TopoSortedPackageList<PackageUninstallDetail> packageUninstallDetails = [];

        foreach (PackageIdentifier identifier in packageSpecifiersToUninstallSpecified)
        {
            PackageLock.Package? package = await _packageManager.GetPackageFromLock(
                identifier);

            if (package is null)
            {
                _context.Logger.LogWarning(
                    "Package '{identifier}' is not installed. Skipping.",
                    identifier);
                continue;
            }

            packageUninstallDetails.Add(new PackageUninstallDetail
            {
                Package = package
            });
        }

        // Uninstall packages in topological order.

        foreach (PackageUninstallDetail packageUninstallDetail in packageUninstallDetails)
        {
            await _packageManager.UninstallPackage(
                packageUninstallDetail.Specifier.Identifier,
                dryRun,
                ignoreScripts);
        }
    }
}