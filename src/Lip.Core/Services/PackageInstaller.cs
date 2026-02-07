using Lip.Core.Entities;
using Lip.Core.FileSources;

namespace Lip.Core.Services;

public interface IPackageInstaller
{
    Task InstallPackage(
        PackageSpec packageSpec,
        IFileSource fileSource,
        bool dryRun,
        bool explicitInstall,
        bool ignoreScripts);

    Task UninstallPackage(
        PackageId packageId,
        bool dryRun,
        bool ignoreScripts);
}