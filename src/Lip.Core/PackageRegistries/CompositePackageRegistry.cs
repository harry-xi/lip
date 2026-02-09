using Lip.Core.Entities;
using Semver;
using System.Collections.Concurrent;

namespace Lip.Core.PackageRegistries;

public class CompositePackageRegistry(IEnumerable<IPackageRegistry> registries) : IPackageRegistry
{
    private readonly IEnumerable<IPackageRegistry> _registries = registries;

    public async Task<IEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId)
    {
        ConcurrentBag<Exception> exceptions = [];

        IEnumerable<Task<IEnumerable<SemVersion>>> tasks = _registries.Select(async r =>
        {
            try
            {
                return await r.GetAvailableVersions(packageId);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                return [];
            }
        });

        IEnumerable<SemVersion>[] taskResults = await Task.WhenAll(tasks);

        // Throw if all registries failed.
        if (exceptions.Count == _registries.Count())
        {
            throw new AggregateException(
                $"Failed to retrieve available versions for package {packageId} from any registry.", exceptions);
        }

        IEnumerable<SemVersion> versions = taskResults
            .SelectMany(v => v)
            .Distinct()
            .Order();

        return versions;
    }

    public async Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec)
    {
        List<Exception> exceptions = [];

        foreach (var registry in _registries)
        {
            try
            {
                return await registry.GetPackageManifest(packageSpec);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException(
            $"Failed to retrieve package manifest for package {packageSpec} from any registry.", exceptions);
    }
}