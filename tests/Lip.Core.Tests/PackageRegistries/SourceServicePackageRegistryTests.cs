using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Lip.Core.Sources;
using Moq;
using Semver;

namespace Lip.Core.Tests.PackageRegistries;

public class SourceServicePackageRegistryTests {
  [Fact]
  public async Task GetAvailableVersions_ThrowsNotSupportedException() {
    // Arrange
    Mock<ISourceService> mockSourceService = new();
    SourceServicePackageRegistry registry = new(mockSourceService.Object);
    PackageId pkgId = new("github.com/test/pkg", "");

    // Act & Assert
    await Assert.ThrowsAsync<NotSupportedException>(() => registry.GetAvailableVersions(pkgId));
  }

  [Fact]
  public async Task GetPackageManifest_RetrievesAndDeserializesManifest() {
    // Arrange
    PackageSpec pkgSpec = new(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));
    string manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/test/pkg",
                "version": "1.0.0",
                "info": { "name": "Test Package" },
                "variants": []
            }
            """;
    MemoryStream manifestStream = new(System.Text.Encoding.UTF8.GetBytes(manifestJson));

    Mock<ISource> mockSource = new();
    mockSource.Setup(p => p.OpenRead("tooth.json")).ReturnsAsync(manifestStream);

    Mock<ISourceService> mockSourceService = new();
    mockSourceService.Setup(s => s.Get(pkgSpec)).ReturnsAsync(mockSource.Object);

    SourceServicePackageRegistry registry = new(mockSourceService.Object);

    // Act
    PackageManifest result = await registry.GetPackageManifest(pkgSpec);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("github.com/test/pkg", result.Path);
    Assert.Equal(new SemVersion(1, 0, 0), result.Version);
  }
}