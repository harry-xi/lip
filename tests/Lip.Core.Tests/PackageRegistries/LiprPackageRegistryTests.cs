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
        LiprPackageRegistry registry = new();
        await Assert.ThrowsAsync<NotSupportedException>(() => registry.GetAvailableVersions(PackageId.Parse("github.com/test/repo")));
    }

    [Fact]
    public async Task GetPackageManifest_ReturnsDeserializedManifest()
    {
        // Arrange
        using HttpTest httpTest = new();
        LiprPackageRegistry registry = new();
        PackageId pkgId = new("github.com/foo/bar", "");
        SemVersion version = new(1, 2, 3);
        PackageSpec pkgSpec = new(pkgId, version);

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
        PackageManifest result = await registry.GetPackageManifest(pkgSpec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("github.com/foo/bar", result.Path);
        Assert.Equal(version, result.Version);

        // Verify URL
        httpTest.ShouldHaveCalled($"https://lipr.levimc.org/{pkgId.Path}/v{version}/tooth.json");
    }
}