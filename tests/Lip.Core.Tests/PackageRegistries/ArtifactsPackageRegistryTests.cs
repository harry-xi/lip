using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Lip.Core.SourceProviders;
using Moq;
using Semver;
using System.Text.Json;
using Xunit;

namespace Lip.Core.Tests.PackageRegistries;

public class ArtifactsPackageRegistryTests
{
    [Fact]
    public async Task GetAvailableVersions_ReturnsOnlyMatchingPackageVersions()
    {
        // Arrange
        var pkgId1 = new PackageId("github.com/foo/bar", "");
        var pkgId2 = new PackageId("github.com/foo/baz", "");

        var artifact1 = new PackageArtifact(new PackageSpec(pkgId1, new SemVersion(1, 0, 0)), Mock.Of<ISourceProvider>());
        var artifact2 = new PackageArtifact(new PackageSpec(pkgId1, new SemVersion(1, 1, 0)), Mock.Of<ISourceProvider>());
        var artifact3 = new PackageArtifact(new PackageSpec(pkgId2, new SemVersion(2, 0, 0)), Mock.Of<ISourceProvider>());

        var registry = new ArtifactsPackageRegistry([artifact1, artifact2, artifact3]);

        // Act
        var result = (await registry.GetAvailableVersions(pkgId1)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(new SemVersion(1, 0, 0), result);
        Assert.Contains(new SemVersion(1, 1, 0), result);
        Assert.DoesNotContain(new SemVersion(2, 0, 0), result);
    }

    [Fact]
    public async Task GetPackageManifest_ValidSpec_ReturnsDeserializedManifest()
    {
        // Arrange
        var pkgId = new PackageId("github.com/foo/bar", "");
        var version = new SemVersion(1, 0, 0);
        var pkgSpec = new PackageSpec(pkgId, version);

        var manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/foo/bar",
                "version": "1.0.0",
                "info": { "name": "Test Package" },
                "variants": []
            }
            """;
        var manifestStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(manifestJson));

        var mockSourceProvider = new Mock<ISourceProvider>();
        mockSourceProvider.Setup(p => p.OpenRead("tooth.json")).ReturnsAsync(manifestStream);

        var artifact = new PackageArtifact(pkgSpec, mockSourceProvider.Object);
        var registry = new ArtifactsPackageRegistry([artifact]);

        // Act
        var result = await registry.GetPackageManifest(pkgSpec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("github.com/foo/bar", result.Path);
        Assert.Equal(version, result.Version);
    }

    [Fact]
    public async Task GetPackageManifest_NotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var pkgSpec = new PackageSpec(new PackageId("github.com/foo/bar", ""), new SemVersion(1, 0, 0));
        var registry = new ArtifactsPackageRegistry([]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => registry.GetPackageManifest(pkgSpec));
    }
}