using Lip.Core.Entities;
using Microsoft.Extensions.Logging;
using Semver;
using System.Collections.Concurrent;

namespace Lip.Core.PackageRegistries;

public class CompositePackageRegistry(IEnumerable<IPackageRegistry> registries) : IPackageRegistry
{
    private readonly IEnumerable<IPackageRegistry> _registries = registries;

    public async Task<IEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId)
    {
        ConcurrentBag<Exception> exceptions = [];

        var tasks = _registries.Select(async r =>
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

        var taskResults = await Task.WhenAll(tasks);

        // Throw if all registries failed.
        if (exceptions.Count == _registries.Count())
        {
            throw new AggregateException(exceptions);
        }

        IEnumerable<SemVersion> versions = taskResults
            .SelectMany(v => v)
            .Distinct()
            .Order();

        return versions;
    }

    public async Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec)
    {
        ConcurrentBag<Exception> exceptions = [];

        var tasks = _registries.Select(async r =>
        {
            try
            {
                return await r.GetPackageManifest(packageSpec);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                return null;
            }
        });

        var taskResults = await Task.WhenAll(tasks);

        // Throw if all registries failed.
        if (exceptions.Count == _registries.Count())
        {
            throw new AggregateException(exceptions);
        }

        PackageManifest? manifest = taskResults.FirstOrDefault(m => m is not null)
            ?? throw new Exception("Failed to retrieve package manifest from any registry.");

        return manifest;
    }
}