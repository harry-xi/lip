using Flurl;
using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Semver;

namespace Lip.Core.RegressionTests.PackageRegistries;

public class GoModuleProxyPackageRegistryTests {
  [Theory]
  [InlineData("github.com/LiteLDev/bds", 157)]
  [InlineData("github.com/LiteLDev/LegacyScriptEngine", 110)]
  [InlineData("github.com/LiteLDev/LeviLamina", 85)]
  [InlineData("github.com/LiteLDev/LeviLamina#client", 10)]
  public async Task GetAvailableVersions_ReturnsSortedMinCount(string packageId, int minVersionCount) {
    GoModuleProxyPackageRegistry registry = new(new Url("https://goproxy.io"));

    IOrderedEnumerable<SemVersion> versions = await registry.GetAvailableVersions(PackageId.Parse(packageId));

    Assert.True(versions.Count() >= minVersionCount);
    Assert.Equal(versions, versions.Order(SemVersion.PrecedenceComparer));
  }
}