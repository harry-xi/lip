using Semver;

namespace Lip.Core.PackageRegistries;

public class CompositeRegistry(
    IReadOnlyList<IPackageRegistry> registries) : IPackageRegistry
{
    private readonly IReadOnlyList<IPackageRegistry> _registries = registries;

    public async Task<PackageManifest> GetManifest(PackageSpecifier packageSpecifier)
    {
        List<Exception> exceptions = [];

        foreach (var registry in _registries)
        {
            try
            {
                return await registry.GetManifest(packageSpecifier);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException("Failed to get manifest from all registries.", exceptions);
    }

    public async Task<List<SemVersion>> GetVersions(PackageIdentifier packageIdentifier)
    {
        List<Exception> exceptions = [];

        foreach (var registry in _registries)
        {
            try
            {
                return await registry.GetVersions(packageIdentifier);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException("Failed to get versions from all registries.", exceptions);
    }
}