using Flurl.Http.Testing;
using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Semver;

namespace Lip.Core.Tests.PackageRegistries;

public class LiprPackageRegistryTests
{
    [Fact]
    public async Task GetAvailableVersions_ThrowsNotSupportedException()
    {
        var registry = new LiprPackageRegistry();
        await Assert.ThrowsAsync<NotSupportedException>(() => registry.GetAvailableVersions(PackageId.Parse("github.com/test/repo")));
    }

    [Fact]
    public async Task GetPackageManifest_ReturnsDeserializedManifest()
    {
        // Arrange
        using var httpTest = new HttpTest();
        var registry = new LiprPackageRegistry();
        var pkgId = new PackageId("github.com/foo/bar", "");
        var version = new SemVersion(1, 2, 3);
        var pkgSpec = new PackageSpec(pkgId, version);

        httpTest.RespondWith("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/foo/bar",
                "version": "1.2.3",
                "info": { "name": "Test Package" },
                "variants": []
            }
            """);

        // Act
        var result = await registry.GetPackageManifest(pkgSpec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("github.com/foo/bar", result.Path);
        Assert.Equal(version, result.Version);

        // Verify URL
        httpTest.ShouldHaveCalled($"https://lipr.levimc.org/{pkgId.Path}/v{version}/tooth.json");
    }
}