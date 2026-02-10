using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Lip.Core.SourceProviders;
using Microsoft.Extensions.Logging;
using Moq;
using Semver;

namespace Lip.Core.Tests.Services;

public class InstallServiceTests
{
    private readonly Mock<ILogger<InstallService>> _mockLogger;
    private readonly Mock<IPackageInstaller> _mockPackageInstaller;
    private readonly Mock<IPackageRegistry> _mockPackageRegistry;
    private readonly Mock<ISourceService> _mockSourceService;
    private readonly Mock<IWorkspaceService> _mockWorkspaceService;
    private readonly InstallService _service;

    public InstallServiceTests()
    {
        _mockLogger = new Mock<ILogger<InstallService>>();
        _mockPackageInstaller = new Mock<IPackageInstaller>();
        _mockPackageRegistry = new Mock<IPackageRegistry>();
        _mockSourceService = new Mock<ISourceService>();
        _mockWorkspaceService = new Mock<IWorkspaceService>();

        _mockPackageRegistry.Setup(r => r.GetAvailableVersions(It.IsAny<PackageId>()))
            .ReturnsAsync(Enumerable.Empty<SemVersion>().OrderBy(v => v));

        _service = new InstallService(
            _mockLogger.Object,
            _mockPackageInstaller.Object,
            _mockPackageRegistry.Object,
            _mockSourceService.Object,
            _mockWorkspaceService.Object);
    }

    [Fact]
    public async Task InstallPackages_NoDependencies_InstallsArtifacts()
    {
        // Arrange
        var pkgId = new PackageId("github.com/test/pkg", string.Empty);
        var pkgVer = new SemVersion(1, 0, 0);
        var pkgSpec = new PackageSpec(pkgId, pkgVer);

        var mockSourceProvider = new Mock<ISourceProvider>();
        _mockSourceService.Setup(s => s.Get(pkgSpec))
            .ReturnsAsync(mockSourceProvider.Object);

        // Act
        await _service.InstallPackages(
            packages: [pkgSpec],
            flexiblePackages: [],
            localPackages: [],
            remotePackages: [],
            dryRun: false,
            ignoreScripts: false,
            noDependencies: true);

        // Assert
        _mockPackageInstaller.Verify(i => i.InstallPackage(
            It.Is<PackageArtifact>(pa => pa.Spec == pkgSpec && pa.SourceProvider == mockSourceProvider.Object),
            false,
            true,
            false), Times.Once);
    }

    [Fact]
    public async Task InstallPackages_WithDependencies_InstallsAllPackages()
    {
        // Arrange
        var pkgRootId = new PackageId("github.com/test/root", string.Empty);
        var pkgRootVer = new SemVersion(1, 0, 0);
        var pkgRootSpec = new PackageSpec(pkgRootId, pkgRootVer);

        var pkgDepId = new PackageId("github.com/test/dep", string.Empty);
        var pkgDepVer = new SemVersion(2, 0, 0);
        var pkgDepSpec = new PackageSpec(pkgDepId, pkgDepVer);

        // Root package manifest with dependency
        var rootManifestJson = $$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/test/root",
                "version": "1.0.0",
                "variants": [
                    {
                        "dependencies": {
                            "github.com/test/dep": "2.0.0"
                        }
                    }
                ]
            }
            """;
        var rootProvider = new Mock<ISourceProvider>();
        rootProvider.Setup(p => p.OpenRead("tooth.json"))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rootManifestJson))));

        // Dep package manifest
        var depManifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/test/dep",
                "version": "2.0.0",
                "variants": [ {} ]
            }
            """;
        var depProvider = new Mock<ISourceProvider>();
        depProvider.Setup(p => p.OpenRead("tooth.json"))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(depManifestJson))));

        _mockSourceService.Setup(s => s.Get(pkgRootSpec)).ReturnsAsync(rootProvider.Object);
        _mockSourceService.Setup(s => s.Get(pkgDepSpec)).ReturnsAsync(depProvider.Object);

        var depManifest = new PackageManifest
        {
            Path = "github.com/test/dep",
            Version = new SemVersion(2, 0, 0),
            Variants = [new()]
        };

        _mockPackageRegistry.Setup(r => r.GetAvailableVersions(It.IsAny<PackageId>()))
            .ReturnsAsync((PackageId id) => (id.ToString().Contains("dep") ? new[] { pkgDepVer } : Array.Empty<SemVersion>()).OrderBy(v => v));

        _mockPackageRegistry.Setup(r => r.GetPackageManifest(It.IsAny<PackageSpec>()))
            .Returns((PackageSpec s) =>
            {
                if (s.Id.ToString().Contains("dep")) return Task.FromResult(depManifest);
                return Task.FromException<PackageManifest>(new KeyNotFoundException());
            });

        // Act
        await _service.InstallPackages(
            packages: [pkgRootSpec],
            flexiblePackages: [],
            localPackages: [],
            remotePackages: [],
            dryRun: false,
            ignoreScripts: false,
            noDependencies: false);

        // Assert
        _mockPackageInstaller.Verify(i => i.InstallPackage(
            It.Is<PackageArtifact>(pa => pa.Spec == pkgDepSpec),
            false,
            false, // implicit
            false), Times.Once);

        _mockPackageInstaller.Verify(i => i.InstallPackage(
            It.Is<PackageArtifact>(pa => pa.Spec == pkgRootSpec),
            false,
            true, // explicit
            false), Times.Once);
    }

    [Fact]
    public async Task InstallPackages_DryRun_DoesNotInstallButLogs()
    {
        // Arrange
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));
        var mockSourceProvider = new Mock<ISourceProvider>();

        var manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/test/pkg",
                "version": "1.0.0",
                "variants": [ {} ]
            }
            """;
        mockSourceProvider.Setup(p => p.OpenRead("tooth.json"))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(manifestJson))));

        _mockSourceService.Setup(s => s.Get(pkgSpec)).ReturnsAsync(mockSourceProvider.Object);

        // Act
        await _service.InstallPackages([pkgSpec], [], [], [], dryRun: true, false, false);

        // Assert
        _mockPackageInstaller.Verify(i => i.InstallPackage(
            It.IsAny<PackageArtifact>(),
            true, // dryRun
            It.IsAny<bool>(),
            It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task UninstallPackages_Explicit_UninstallsPackage()
    {
        // Arrange
        var pkgId = new PackageId("github.com/test/pkg", "");
        var pkgVersion = new SemVersion(1, 0, 0);
        var pkgSpec = new PackageSpec(pkgId, pkgVersion);

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit))
            .ReturnsAsync([pkgSpec]);
        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([pkgSpec]);

        // Act
        await _service.UninstallPackages([pkgId], false, false, false);

        // Assert
        _mockPackageInstaller.Verify(i => i.UninstallPackage(pkgId, false, false), Times.Once);
    }

    [Fact]
    public async Task UninstallPackages_WithDependencies_UninstallsUnusedDependencies()
    {
        // Arrange
        var rootId = new PackageId("github.com/test/root", "");
        var depId = new PackageId("github.com/test/dep", "");

        var rootSpec = new PackageSpec(rootId, new SemVersion(1, 0, 0));
        var depSpec = new PackageSpec(depId, new SemVersion(1, 0, 0));

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit))
            .ReturnsAsync([rootSpec]);
        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([depSpec, rootSpec]);

        // Act
        await _service.UninstallPackages([rootId], false, false, false);

        // Assert
        _mockPackageInstaller.Verify(i => i.UninstallPackage(rootId, false, false), Times.Once);
        _mockPackageInstaller.Verify(i => i.UninstallPackage(depId, false, false), Times.Once);
    }

    [Fact]
    public async Task UninstallPackages_NotInstalled_ThrowsException()
    {
        var pkgId = new PackageId("github.com/test/pkg", "");
        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit))
            .ReturnsAsync([]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UninstallPackages([pkgId], false, false, false));
    }

    [Fact]
    public async Task InstallPackages_FlexibleVersion_ResolvesLatest()
    {
        var pkgId = new PackageId("github.com/flexible/pkg", "");
        var v1 = new SemVersion(1, 0, 0);
        var v2 = new SemVersion(2, 0, 0);
        var pkgSpecV2 = new PackageSpec(pkgId, v2);

        _mockPackageRegistry.Setup(r => r.GetAvailableVersions(pkgId))
            .ReturnsAsync(new[] { v1, v2 }.Order(SemVersion.PrecedenceComparer));

        var mockSourceProvider = new Mock<ISourceProvider>();
        // Mock manifest for v2
        var manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/flexible/pkg",
                "version": "2.0.0",
                "variants": [{}]
            }
            """;
        mockSourceProvider.Setup(p => p.OpenRead("tooth.json"))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(manifestJson))));

        _mockSourceService.Setup(s => s.Get(pkgSpecV2)).ReturnsAsync(mockSourceProvider.Object);

        await _service.InstallPackages([], [pkgId], [], [], false, false, false);

        _mockPackageInstaller.Verify(i => i.InstallPackage(
            It.Is<PackageArtifact>(pa => pa.Spec.Version == v2),
            false,
            true,
            false), Times.Once);
    }

    [Fact]
    public async Task InstallPackages_AlreadyExplicitlyInstalled_ThrowsException()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/pkg", ""), new SemVersion(1));
        var mockSourceProvider = new Mock<ISourceProvider>();
        _mockSourceService.Setup(s => s.Get(pkgSpec)).ReturnsAsync(mockSourceProvider.Object);

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit))
            .ReturnsAsync([pkgSpec]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InstallPackages([pkgSpec], [], [], [], false, false, false));
    }

    [Fact]
    public async Task InstallPackages_DuplicateIds_ThrowsException()
    {
        var pkgSpec1 = new PackageSpec(new PackageId("github.com/test/pkg", ""), new SemVersion(1));
        var pkgSpec2 = new PackageSpec(new PackageId("github.com/test/pkg", ""), new SemVersion(2));

        var mockSourceProvider = new Mock<ISourceProvider>();
        _mockSourceService.Setup(s => s.Get(It.IsAny<PackageSpec>())).ReturnsAsync(mockSourceProvider.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InstallPackages([pkgSpec1, pkgSpec2], [], [], [], false, false, true)); // noDependencies=true to skip solver
    }

    [Fact]
    public async Task UpdatePackages_UpdatesExistingPackage()
    {
        var pkgId = new PackageId("github.com/test/pkg", "");
        var oldVer = new SemVersion(1);
        var newVer = new SemVersion(2);

        var oldSpec = new PackageSpec(pkgId, oldVer);
        var newSpec = new PackageSpec(pkgId, newVer);

        // Setup installed state
        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([oldSpec]);
        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit))
            .ReturnsAsync([oldSpec]);

        // Setup new package resolution
        var mockSourceProvider = new Mock<ISourceProvider>();
        var manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/test/pkg",
                "version": "2.0.0",
                "variants": [{}]
            }
            """;
        mockSourceProvider.Setup(p => p.OpenRead("tooth.json"))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(manifestJson))));
        _mockSourceService.Setup(s => s.Get(newSpec)).ReturnsAsync(mockSourceProvider.Object);

        // Act
        await _service.UpdatePackages([newSpec], [], [], [], false, false);

        // Assert
        // Should verify uninstall of old package and install of new package
        _mockPackageInstaller.Verify(i => i.UninstallPackage(pkgId, false, false), Times.Once);

        _mockPackageInstaller.Verify(i => i.InstallPackage(
            It.Is<PackageArtifact>(pa => pa.Spec == newSpec),
            false,
            true,
            false), Times.Once);
    }

    [Fact]
    public async Task UpdatePackages_NotInstalled_ThrowsException()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/pkg", ""), new SemVersion(2));
        var mockSourceProvider = new Mock<ISourceProvider>();
        _mockSourceService.Setup(s => s.Get(pkgSpec)).ReturnsAsync(mockSourceProvider.Object);

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([]); // Nothing installed

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdatePackages([pkgSpec], [], [], [], false, false));
    }
}