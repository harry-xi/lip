using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Lip.Core.SourceProviders;
using Moq;
using Semver;
using Xunit;

namespace Lip.Core.Tests.PackageRegistries;

public class SourceServicePackageRegistryTests
{
    [Fact]
    public async Task GetAvailableVersions_ThrowsNotSupportedException()
    {
        // Arrange
        var mockSourceService = new Mock<ISourceService>();
        var registry = new SourceServicePackageRegistry(mockSourceService.Object);
        var pkgId = new PackageId("github.com/test/pkg", "");

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => registry.GetAvailableVersions(pkgId));
    }

    [Fact]
    public async Task GetPackageManifest_RetrievesAndDeserializesManifest()
    {
        // Arrange
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));
        var manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/test/pkg",
                "version": "1.0.0",
                "info": { "name": "Test Package" },
                "variants": []
            }
            """;
        var manifestStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(manifestJson));

        var mockSourceProvider = new Mock<ISourceProvider>();
        mockSourceProvider.Setup(p => p.OpenRead("tooth.json")).ReturnsAsync(manifestStream);

        var mockSourceService = new Mock<ISourceService>();
        mockSourceService.Setup(s => s.Get(pkgSpec)).ReturnsAsync(mockSourceProvider.Object);

        var registry = new SourceServicePackageRegistry(mockSourceService.Object);

        // Act
        var result = await registry.GetPackageManifest(pkgSpec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("github.com/test/pkg", result.Path);
        Assert.Equal(new SemVersion(1, 0, 0), result.Version);
    }
}