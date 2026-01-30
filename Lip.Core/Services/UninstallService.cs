using Microsoft.Extensions.Logging;

namespace Lip.Core.Services;

public class UninstallService(IContext context, IPackageManager packageManager)
{
    private readonly IContext _context = context;
    private readonly IPackageManager _packageManager = packageManager;

    public record Args
    {
        public required bool DryRun { get; init; }
        public required bool IgnoreScripts { get; init; }
    }

    public async Task Uninstall(List<string> packageSpecifierTextsToUninstall, Args args)
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
                args.DryRun,
                args.IgnoreScripts);
        }
    }
}