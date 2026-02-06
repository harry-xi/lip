using Lip.Core.Entities;
using Semver;

namespace Lip.Core.Registries;

public interface IRegistry
{
    Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec);
    Task<IEnumerable<SemVersion>> GetPackageVersions(PackageId packageId);
}