using Flurl;
using Lip.Core.Entities;
using System.IO.Abstractions;

namespace Lip.Core.Services;

public interface IInstallService
{
    Task InstallPackage(
        IEnumerable<PackageSpec> packages,
        IEnumerable<PackageId> flexiblePackages,
        IEnumerable<IFileInfo> localPackages,
        IEnumerable<Url> remotePackages,
        bool dryRun,
        bool ignoreScripts,
        bool noDependencies);

    Task UninstallPackage(
        IEnumerable<PackageId> packages,
        bool dryRun,
        bool ignoreScripts,
        bool noDependencies);

    Task UpdatePackage(
        IEnumerable<PackageSpec> packages,
        IEnumerable<PackageId> flexiblePackages,
        bool dryRun,
        bool ignoreScripts);
}