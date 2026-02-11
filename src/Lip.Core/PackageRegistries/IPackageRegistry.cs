using Lip.Core.Entities;
using Semver;

namespace Lip.Core.PackageRegistries;

public interface IPackageRegistry
{
    /// <returns>
    /// A sorted list of available versions for the given package ID (oldest first).
    /// </returns>
    Task<IOrderedEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId);
    Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec);
}