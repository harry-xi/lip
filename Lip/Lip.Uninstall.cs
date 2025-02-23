using Microsoft.Extensions.Logging;
using Semver;
using System.Runtime.InteropServices;

namespace Lip;

public partial class Lip
{
    public record UninstallArgs
    {
        public required bool DryRun { get; init; }
        public required bool IgnoreScripts { get; init; }
        public required bool Save { get; init; }
    }

    private record PackageUninstallDetail : TopoSortedPackageList<PackageUninstallDetail>.IItem
    {
        public Dictionary<PackageSpecifierWithoutVersion, SemVersionRange> Dependencies
        {
            get
            {
                return Manifest.GetSpecifiedVariant(
                        VariantLabel,
                        RuntimeInformation.RuntimeIdentifier)?
                        .Dependencies?
                        .Select(
                            kvp => new KeyValuePair<PackageSpecifierWithoutVersion, SemVersionRange>(
                                PackageSpecifierWithoutVersion.Parse(kvp.Key),
                                SemVersionRange.ParseNpm(kvp.Value)))
                        .ToDictionary()
                        ?? [];
            }
        }

        public required PackageManifest Manifest { get; init; }

        public PackageSpecifier Specifier => new()
        {
            ToothPath = Manifest.ToothPath,
            VariantLabel = VariantLabel,
            Version = Manifest.Version
        };

        public required string VariantLabel { get; init; }
    }

    public async Task Uninstall(List<string> packageSpecifierTextsToUninstall, UninstallArgs args)
    {
        List<PackageSpecifierWithoutVersion> packageSpecifiersToUninstallSpecified =
            packageSpecifierTextsToUninstall.ConvertAll(PackageSpecifierWithoutVersion.Parse);

        // Remove non-installed packages and sort packages topologically.

        TopoSortedPackageList<PackageUninstallDetail> packageUninstallDetails = [];

        foreach (PackageSpecifierWithoutVersion packageSpecifier in packageSpecifiersToUninstallSpecified)
        {
            PackageManifest? packageManifest = await _packageManager.GetPackageManifestFromInstalledPackages(
                packageSpecifier);

            if (packageManifest is null)
            {
                _context.Logger.LogWarning(
                    "Package '{packageSpecifier}' is not installed. Skipping.",
                    packageSpecifier);
                continue;
            }

            packageUninstallDetails.Add(new PackageUninstallDetail
            {
                Manifest = packageManifest,
                VariantLabel = packageSpecifier.VariantLabel
            });
        }

        // Uninstall packages in topological order.

        foreach (PackageUninstallDetail packageUninstallDetail in packageUninstallDetails)
        {
            await _packageManager.UninstallPackage(
                packageUninstallDetail.Specifier,
                args.DryRun,
                args.IgnoreScripts);
        }

        // If to save, update the package manifest file.

        if (args.Save)
        {
            PackageManifest packageManifest = await _packageManager.GetCurrentPackageManifestWithTemplate()
                ?? throw new InvalidOperationException("Package manifest is not found.");

            PackageManifest newPackagemanifest = packageManifest with
            {
                Variants = packageManifest.Variants?
                    .ConvertAll(variant =>
                    {
                        if (!variant.Match(
                            string.Empty,
                            RuntimeInformation.RuntimeIdentifier))
                        {
                            return variant;
                        }

                        return variant with
                        {
                            Dependencies = variant.Dependencies?
                                .Where(dependency => !packageSpecifiersToUninstallSpecified.Any(
                                    packageSpecifier => packageSpecifier.ToString() == dependency.Key))
                                .ToDictionary()
                        };
                    })
            };

            if (!args.DryRun)
            {
                await _packageManager.SaveCurrentPackageManifest(newPackagemanifest);
            }
        }
    }
}
