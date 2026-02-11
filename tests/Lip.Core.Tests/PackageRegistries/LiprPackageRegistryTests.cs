using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Moq;
using Semver;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.PackageRegistries;

public class LiprPackageRegistryTests
{
    [Fact]
    public async Task GetAvailableVersions_ThrowsNotSupportedException()
    {
        var mockDownloader = new Mock<IFileDownloader>();
        var mockCache = new Mock<ICacheService>();
        LiprPackageRegistry registry = new(mockDownloader.Object, mockCache.Object);
        await Assert.ThrowsAsync<NotSupportedException>(() => registry.GetAvailableVersions(PackageId.Parse("github.com/test/repo")));
    }

    [Fact]
    public async Task GetPackageManifest_ReturnsDeserializedManifest()
    {
        // Arrange
        var mockDownloader = new Mock<IFileDownloader>();
        var mockCache = new Mock<ICacheService>();
        LiprPackageRegistry registry = new(mockDownloader.Object, mockCache.Object);

        PackageId pkgId = new("github.com/foo/bar", "");
        SemVersion version = new(1, 2, 3);
        PackageSpec pkgSpec = new(pkgId, version);

        string manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/foo/bar",
                "version": "1.2.3",
                "info": { "name": "Test Package" },
                "variants": []
            }
            """;

        var mockFileSystem = new MockFileSystem();
        var mockFile = new Mock<IFileInfo>();

        // Setup OpenRead to return a stream from the mock file system
        var path = @"C:\fake\path\tooth.json";
        mockFileSystem.AddFile(path, new MockFileData(manifestJson));
        mockFile.Setup(f => f.OpenRead())
            .Returns(mockFileSystem.File.OpenRead(path));

        mockCache.Setup(c => c.GetOrCreateFile(It.IsAny<string>(), It.IsAny<Func<IFileInfo, Task>>()))
            .Callback<string, Func<IFileInfo, Task>>((key, factory) => factory(mockFile.Object))
            .ReturnsAsync(mockFile.Object);

        // Act
        PackageManifest result = await registry.GetPackageManifest(pkgSpec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("github.com/foo/bar", result.Path);
        Assert.Equal(version, result.Version);

        // Verify that GetOrCreateFile was called with the correct URL
        string expectedUrl = $"https://lipr.levimc.org/{pkgId.Path}/v{version}/tooth.json";
        mockCache.Verify(c => c.GetOrCreateFile(
            expectedUrl,
            It.IsAny<Func<IFileInfo, Task>>()),
            Times.Once);
    }
}