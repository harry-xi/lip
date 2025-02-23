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

    public async Task Uninstall(List<string> packageSpecifierTexts, UninstallArgs args)
    {
        List<PackageSpecifierWithoutVersion> packageSpecifiersToUninstall = packageSpecifierTexts
            .ConvertAll(PackageSpecifierWithoutVersion.Parse);

        // Check if all packages to uninstall are installed.

        foreach (PackageSpecifierWithoutVersion packageSpecifier in packageSpecifiersToUninstall)
        {
            if (await _packageManager.GetPackageManifestFromInstalledPackages(packageSpecifier) is null)
            {
                throw new InvalidOperationException($"Package '{packageSpecifier}' is not installed.");
            }
        }

        // Uninstall all packages.

        foreach (PackageSpecifierWithoutVersion packageSpecifier in packageSpecifiersToUninstall)
        {
            await _packageManager.UninstallPackage(packageSpecifier, args.DryRun, args.IgnoreScripts);
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
                                .Where(dependency => !packageSpecifiersToUninstall.Any(
                                    packageSpecifier => packageSpecifier.Specifier == dependency.Key))
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
