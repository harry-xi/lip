using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Moq;
using Semver;

namespace Lip.Core.Tests.PackageRegistries;

public class WorkspaceServicePackageRegistryTests
{
    [Fact]
    public async Task GetAvailableVersions_PackageInstalled_ReturnsVersion()
    {
        // Arrange
        var pkgId = new PackageId("github.com/test/pkg", "");
        var pkgSpec = new PackageSpec(pkgId, new SemVersion(1, 0, 0));

        var mockWorkspaceService = new Mock<IWorkspaceService>();
        mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([pkgSpec]);

        var registry = new WorkspaceServicePackageRegistry(mockWorkspaceService.Object);

        // Act
        var result = (await registry.GetAvailableVersions(pkgId)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(new SemVersion(1, 0, 0), result[0]);
    }

    [Fact]
    public async Task GetAvailableVersions_PackageNotInstalled_ReturnsEmpty()
    {
        // Arrange
        var pkgId = new PackageId("github.com/test/pkg", "");

        var mockWorkspaceService = new Mock<IWorkspaceService>();
        mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([]);

        var registry = new WorkspaceServicePackageRegistry(mockWorkspaceService.Object);

        // Act
        var result = await registry.GetAvailableVersions(pkgId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPackageManifest_ReturnsFromWorkspaceService()
    {
        // Arrange
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));
        var manifest = new PackageManifest { Path = "github.com/test/pkg", Version = new SemVersion(1, 0, 0) };

        var mockWorkspaceService = new Mock<IWorkspaceService>();
        mockWorkspaceService.Setup(w => w.GetInstalledPackageManifest(pkgSpec)).ReturnsAsync(manifest);

        var registry = new WorkspaceServicePackageRegistry(mockWorkspaceService.Object);

        // Act
        var result = await registry.GetPackageManifest(pkgSpec);

        // Assert
        Assert.Equal(manifest, result);
    }
}