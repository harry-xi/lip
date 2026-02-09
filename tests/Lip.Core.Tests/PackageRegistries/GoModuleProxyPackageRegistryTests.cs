using Flurl;
using Flurl.Http.Testing;
using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Semver;

namespace Lip.Core.Tests.PackageRegistries;

public class GoModuleProxyPackageRegistryTests
{
    [Fact]
    public async Task GetAvailableVersions_ParsesResponseCorrectly()
    {
        // Arrange
        using var httpTest = new HttpTest();
        var proxyUrl = Url.Parse("https://proxy.golang.org");
        var registry = new GoModuleProxyPackageRegistry(proxyUrl);
        var pkgId = new PackageId("github.com/test/pkg", "");

        httpTest.RespondWith("v1.0.0\nv1.1.0\nv2.0.0-rc.1\ninvalid-version\n");

        // Act
        var result = (await registry.GetAvailableVersions(pkgId)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(SemVersion.Parse("1.0.0"), result);
        Assert.Contains(SemVersion.Parse("1.1.0"), result);
        Assert.Contains(SemVersion.Parse("2.0.0-rc.1"), result);
    }

    [Fact]
    public async Task GetPackageManifest_ThrowsNotSupportedException()
    {
        var registry = new GoModuleProxyPackageRegistry(new Url("https://proxy"));
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            registry.GetPackageManifest(new PackageSpec(PackageId.Parse("github.com/test/repo"), new SemVersion(1, 0, 0))));
    }
}