using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Lip.Core.Sources;
using Moq;
using Semver;

namespace Lip.Core.Tests.PackageRegistries;

public class ArtifactsPackageRegistryTests {
  [Fact]
  public async Task GetAvailableVersions_ReturnsOnlyMatchingPackageVersions() {
    // Arrange
    PackageId pkgId1 = new("github.com/foo/bar", "");
    PackageId pkgId2 = new("github.com/foo/baz", "");

    PackageArtifact artifact1 = new(new PackageSpec(pkgId1, new SemVersion(1, 0, 0)), Mock.Of<ISource>());
    PackageArtifact artifact2 = new(new PackageSpec(pkgId1, new SemVersion(1, 1, 0)), Mock.Of<ISource>());
    PackageArtifact artifact3 = new(new PackageSpec(pkgId2, new SemVersion(2, 0, 0)), Mock.Of<ISource>());

    ArtifactsPackageRegistry registry = new([artifact1, artifact2, artifact3]);

    // Act
    List<SemVersion> result = [.. (await registry.GetAvailableVersions(pkgId1))];

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Contains(new SemVersion(1, 0, 0), result);
    Assert.Contains(new SemVersion(1, 1, 0), result);
    Assert.DoesNotContain(new SemVersion(2, 0, 0), result);
  }

  [Fact]
  public async Task GetPackageManifest_ValidSpec_ReturnsDeserializedManifest() {
    // Arrange
    PackageId pkgId = new("github.com/foo/bar", "");
    SemVersion version = new(1, 0, 0);
    PackageSpec pkgSpec = new(pkgId, version);

    string manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/foo/bar",
                "version": "1.0.0",
                "info": { "name": "Test Package" },
                "variants": []
            }
            """;
    MemoryStream manifestStream = new(System.Text.Encoding.UTF8.GetBytes(manifestJson));

    Mock<ISource> mockSource = new();
    mockSource.Setup(p => p.OpenRead("tooth.json")).ReturnsAsync(manifestStream);

    PackageArtifact artifact = new(pkgSpec, mockSource.Object);
    ArtifactsPackageRegistry registry = new([artifact]);

    // Act
    PackageManifest result = await registry.GetPackageManifest(pkgSpec);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("github.com/foo/bar", result.Path);
    Assert.Equal(version, result.Version);
  }

  [Fact]
  public async Task GetPackageManifest_NotFound_ThrowsInvalidOperationException() {
    // Arrange
    PackageSpec pkgSpec = new(new PackageId("github.com/foo/bar", ""), new SemVersion(1, 0, 0));
    ArtifactsPackageRegistry registry = new([]);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => registry.GetPackageManifest(pkgSpec));
  }
}