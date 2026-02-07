using Lip.Core.Entities;
using Semver;

namespace Lip.Core.Registries;

public interface IRegistry
{
    Task<IEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId);
    Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec);
}