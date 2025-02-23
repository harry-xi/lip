using Semver;
using System.Runtime.InteropServices;

namespace Lip;

public partial class Lip
{
    public record PruneArgs
    {
        public required bool DryRun { get; init; }
        public required bool IgnoreScripts { get; init; }
    }

    public async Task Prune(PruneArgs args)
    {
        List<PackageSpecifierWithoutVersion> packageSpecifiersUnnecessary = await _dependencySolver
            .GetUnnecessaryPackages();

        // Sort packages topologically.

        TopoSortedPackageList<PackageUninstallDetail> packageUninstallDetails = [];

        foreach (PackageSpecifierWithoutVersion packageSpecifier in packageSpecifiersUnnecessary)
        {
            PackageManifest packageManifest = (await _packageManager.GetPackageManifestFromInstalledPackages(
                packageSpecifier))!; // We know that the package is installed.

            packageUninstallDetails.Add(new PackageUninstallDetail
            {
                Manifest = packageManifest,
                VariantLabel = packageSpecifier.VariantLabel
            });
        }

        // Uninstall all packages in topological order.

        foreach (PackageUninstallDetail packageUninstallDetail in packageUninstallDetails)
        {
            await _packageManager.UninstallPackage(
                packageUninstallDetail.Specifier,
                args.DryRun,
                args.IgnoreScripts);
        }
    }
}
