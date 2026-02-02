using Semver;

namespace Lip.Core.PackageRegistries;

public class CompositeRegistry(
    IReadOnlyList<IPackageRegistry> registries) : IPackageRegistry
{
    private readonly IReadOnlyList<IPackageRegistry> _registries = registries;

    public async Task<PackageManifest?> GetManifest(PackageSpecifier packageSpecifier)
    {
        // Since all registries share the same cache/storage for manifests in the current design,
        // we can just ask the first one, or any one.
        // However, to be safe, we can try them in order if we anticipate different behavior later,
        // but for now, they are identical in GetManifest implementation.
        // Let's just use the first one if available.
        if (_registries.Count > 0)
        {
            return await _registries[0].GetManifest(packageSpecifier);
        }

        return null;
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
                // We don't log here ideally, as the sub-registries should logic warnings?
                // Or maybe we should log debug here.
                // The requirement said "until successful".
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException("Failed to get versions from all registries.", exceptions);
        }

        throw new InvalidOperationException("No registries configured or all failed without exception (unexpected).");
    }
}