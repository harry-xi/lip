using Semver;

namespace Lip.Core.PackageRegistries;

public interface IPackageRegistry
{
    Task<PackageManifest?> GetManifest(PackageSpecifier packageSpecifier);
    Task<List<SemVersion>> GetVersions(PackageIdentifier packageIdentifier);
}