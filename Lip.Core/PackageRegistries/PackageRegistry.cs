using Semver;

namespace Lip.Core.PackageRegistries;

public class PackageRegistry(IPackageManager packageManager) : IPackageRegistry
{
    private readonly IPackageManager _packageManager = packageManager;

    public Task<PackageManifest?> GetManifest(PackageSpecifier packageSpecifier)
    {
        return _packageManager.GetPackageManifestFromCache(packageSpecifier);
    }

    public Task<List<SemVersion>> GetVersions(PackageIdentifier packageIdentifier)
    {
        return _packageManager.GetPackageRemoteVersions(packageIdentifier);
    }
}