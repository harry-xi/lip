using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Moq;
using Semver;

namespace Lip.Core.Tests.PackageRegistries;

public class WorkspaceServicePackageRegistryTests {
  [Fact]
  public async Task GetAvailableVersions_PackageInstalled_ReturnsVersion() {
    // Arrange
    PackageId pkgId = new("github.com/test/pkg", "");
    PackageSpec pkgSpec = new(pkgId, new SemVersion(1, 0, 0));

    Mock<IWorkspaceService> mockWorkspaceService = new();
    mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
        .ReturnsAsync([pkgSpec]);

    WorkspaceServicePackageRegistry registry = new(mockWorkspaceService.Object);

    // Act
    List<SemVersion> result = [.. (await registry.GetAvailableVersions(pkgId))];

    // Assert
    Assert.Single(result);
    Assert.Equal(new SemVersion(1, 0, 0), result[0]);
  }

  [Fact]
  public async Task GetAvailableVersions_PackageNotInstalled_ReturnsEmpty() {
    // Arrange
    PackageId pkgId = new("github.com/test/pkg", "");

    Mock<IWorkspaceService> mockWorkspaceService = new();
    mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
        .ReturnsAsync([]);

    WorkspaceServicePackageRegistry registry = new(mockWorkspaceService.Object);

    // Act
    IOrderedEnumerable<SemVersion> result = await registry.GetAvailableVersions(pkgId);

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public async Task GetPackageManifest_ReturnsFromWorkspaceService() {
    // Arrange
    PackageSpec pkgSpec = new(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));
    PackageManifest manifest = new() { Path = "github.com/test/pkg", Version = new SemVersion(1, 0, 0) };

    Mock<IWorkspaceService> mockWorkspaceService = new();
    mockWorkspaceService.Setup(w => w.GetInstalledPackageManifest(pkgSpec)).ReturnsAsync(manifest);

    WorkspaceServicePackageRegistry registry = new(mockWorkspaceService.Object);

    // Act
    PackageManifest result = await registry.GetPackageManifest(pkgSpec);

    // Assert
    Assert.Equal(manifest, result);
  }
}
