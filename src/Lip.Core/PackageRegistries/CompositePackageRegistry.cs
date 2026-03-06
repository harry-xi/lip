using Lip.Core.Entities;
using Semver;

namespace Lip.Core.PackageRegistries;

public class CompositePackageRegistry(IEnumerable<IEnumerable<IPackageRegistry>> registryGroups) : IPackageRegistry {
  public async Task<IOrderedEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId) {
    List<Exception> exceptions = [];

    List<IEnumerable<SemVersion>> results = [];
    foreach (IEnumerable<IPackageRegistry> registryGroup in registryGroups) {
      foreach (IPackageRegistry registry in registryGroup) {
        try {
          results.Add(await registry.GetAvailableVersions(packageId));
          break; // Stop trying other registries in this group if one succeeds.
        }
        catch (Exception ex) {
          exceptions.Add(ex);
        }
      }
    }

    // Throw if all registries failed.
    if (results.Count == 0 && exceptions.Count > 0) {
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

    foreach (IPackageRegistry registry in registryGroups.SelectMany(g => g)) {
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