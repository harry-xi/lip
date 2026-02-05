using Lip.Core;
using Lip.Core.Context;
using Microsoft.Extensions.Logging;

namespace Lip.Core.Services;

public class UninstallService
{
    private readonly IContext _context;
    private readonly IWorkspaceManager _workspaceManager;

    public UninstallService(IContext context)
    {
        _context = context;

        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);
        var pathManager = ServiceFactory.CreatePathManager(context, runtimeConfig);
        var cacheManager = ServiceFactory.CreateCacheManager(context, pathManager, runtimeConfig);
        _workspaceManager = ServiceFactory.CreateWorkspaceManager(context, pathManager, cacheManager);
    }

    internal UninstallService(IContext context, IWorkspaceManager workspaceManager)
    {
        _context = context;
        _workspaceManager = workspaceManager;
    }

    public async Task Uninstall(
        List<string> packageSpecifierTextsToUninstall,
        bool dryRun = false,
        bool ignoreScripts = false)
    {
        List<PackageIdentifier> packageSpecifiersToUninstallSpecified =
            packageSpecifierTextsToUninstall.ConvertAll(PackageIdentifier.Parse);

        // Remove non-installed packages and sort packages topologically.

        List<PackageDependencyDescriptor> packageDescriptors = [];

        foreach (PackageIdentifier identifier in packageSpecifiersToUninstallSpecified)
        {
            PackageLock.Package? package = await _workspaceManager.GetPackageFromLock(
                identifier);

            if (package is null)
            {
                _context.Logger.LogWarning(
                    "Package '{identifier}' is not installed. Skipping.",
                    identifier);
                continue;
            }

            packageDescriptors.Add(new PackageDependencyDescriptor(
                package.Specifier,
                package.Variant.Dependencies.Select(kv => new PackageRequirement(kv.Key, kv.Value))));
        }

        var sortedPackages = TopologicalSort.Sort(packageDescriptors);

        // Uninstall packages in topological order.

        foreach (var descriptor in sortedPackages)
        {
            await _workspaceManager.UninstallPackage(
                descriptor.Specifier.Identifier,
                dryRun,
                ignoreScripts);
        }

    }
}