using Microsoft.Extensions.Logging;

namespace Lip.Core;

public partial class Lip
{
    public record UpdateArgs
    {
        public required bool DryRun { get; init; }

        public required bool IgnoreScripts { get; init; }
        public required bool NoDependencies { get; init; }
    }

    public async Task Update(List<string> userInputPackageTexts, UpdateArgs args)
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

        await Install(packagesToUpdate, new InstallArgs
        {
            DryRun = args.DryRun,
            IgnoreScripts = args.IgnoreScripts,
            NoDependencies = args.NoDependencies,
            UpgradeLockedPackages = true,
            OverwriteFiles = false,
        });
    }
}