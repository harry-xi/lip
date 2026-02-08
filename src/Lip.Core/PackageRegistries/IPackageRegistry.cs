using Lip.Core.Entities;
using Semver;

namespace Lip.Core.PackageRegistries;

public interface IPackageRegistry
{
    Task<IEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId);
    Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec);
}