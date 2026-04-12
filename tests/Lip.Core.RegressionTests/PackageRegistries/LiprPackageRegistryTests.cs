using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Semver;

namespace Lip.Core.RegressionTests.PackageRegistries;

public class LiprPackageRegistryTests {
  [Theory]
  [InlineData("github.com/LiteLDev/MoreDimensions", 20)]
  [InlineData("github.com/LiteLDev/LegacyScriptEngine", 119)]
  [InlineData("github.com/LiteLDev/LeviLamina", 90)]
  [InlineData("github.com/LiteLDev/LeviLamina#client", 17)]
  public async Task GetAvailableVersions_ReturnsSortedMinCount(string packageId, int minVersionCount) {
    LiprPackageRegistry registry = new();

    IOrderedEnumerable<SemVersion> versions = await registry.GetAvailableVersions(PackageId.Parse(packageId));

    Assert.True(versions.Count() >= minVersionCount);
    Assert.Equal(versions, versions.Order(SemVersion.PrecedenceComparer));
  }

  [Theory]
  [InlineData("github.com/LiteLDev/MoreDimensions", "0.13.0")]
  [InlineData("github.com/LiteLDev/LegacyScriptEngine", "0.17.13")]
  [InlineData("github.com/LiteLDev/LeviLamina", "1.9.9")]
  [InlineData("github.com/LiteLDev/LeviLamina#client", "1.9.9")]
  public async Task GetPackageManifest_ReturnsManifest(string packageId, string version) {
    LiprPackageRegistry registry = new();

    PackageManifest manifest = await registry.GetPackageManifest(
        new PackageSpec(PackageId.Parse(packageId), SemVersion.Parse(version)));

    Assert.Equal(PackageId.Parse(packageId).Path, manifest.Path);
    Assert.Equal(SemVersion.Parse(version), manifest.Version);
  }
}
