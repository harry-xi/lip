using System.Collections.Concurrent;
using Lip.Core.Entities;
using Semver;

namespace Lip.Core.PackageRegistries;

public class CompositePackageRegistry(IEnumerable<IPackageRegistry> registries) : IPackageRegistry {
  private readonly IEnumerable<IPackageRegistry> _registries = registries;

  public async Task<IOrderedEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId) {
    ConcurrentBag<Exception> exceptions = [];

    List<IEnumerable<SemVersion>> results = [];
    foreach (IPackageRegistry registry in _registries) {
      try {
        results.Add(await registry.GetAvailableVersions(packageId));
      }
      catch (Exception ex) {
        exceptions.Add(ex);
      }
    }

    // Throw if all registries failed.
    if (exceptions.Count == _registries.Count()) {
      throw new AggregateException(
          $"Failed to retrieve available versions for package {packageId} from any registry.", exceptions);
    }

    IOrderedEnumerable<SemVersion> versions = results
        .SelectMany(v => v)
        .Distinct()
        .Order(SemVersion.PrecedenceComparer);

    return versions;
  }

  public async Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec) {
    List<Exception> exceptions = [];

    foreach (IPackageRegistry registry in _registries) {
      try {
        return await registry.GetPackageManifest(packageSpec);
      }
      catch (Exception ex) {
        exceptions.Add(ex);
      }
    }

    throw new AggregateException(
        $"Failed to retrieve package manifest for package {packageSpec} from any registry.", exceptions);
  }
}