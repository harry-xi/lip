using System.Text.Json;
using Flurl;
using Flurl.Http;
using Lip.Core.Entities;
using Semver;

namespace Lip.Core.PackageRegistries;

public class LiprPackageRegistry : IPackageRegistry {
  public async Task<IOrderedEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId) {
    Url url = Url.Parse($"https://lipr.levimc.org/index.json");

    using Stream stream = await url.GetStreamAsync();

    PackageIndex index = (await JsonSerializer.DeserializeAsync<PackageIndex>(stream))!;

    return index.Packages[packageId.Path].Variants[packageId.Variant].Versions
        .Order(SemVersion.PrecedenceComparer);
  }

  public async Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec) {
    Url url = Url.Parse(
        $"https://lipr.levimc.org/{packageSpec.Id.Path}@{packageSpec.Version}/tooth.json");

    using Stream manifestStream = await url.GetStreamAsync();

    return await PackageManifest.FromStream(manifestStream);
  }
}