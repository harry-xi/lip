using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Lip.Core.SourceProviders;
using Moq;
using Semver;

namespace Lip.Core.Tests.Services;

public class InstallServiceTests
{
    private readonly Mock<IUserInteraction> _mockLogger;
    private readonly Mock<IPackageInstaller> _mockPackageInstaller;
    private readonly Mock<IPackageRegistry> _mockPackageRegistry;
    private readonly Mock<ISourceService> _mockSourceService;
    private readonly Mock<IWorkspaceService> _mockWorkspaceService;
    private readonly InstallService _service;

    public InstallServiceTests()
    {
        _mockLogger = new Mock<IUserInteraction>();
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
        PackageId pkgId = new("github.com/test/pkg", string.Empty);
        SemVersion pkgVer = new(1, 0, 0);
        PackageSpec pkgSpec = new(pkgId, pkgVer);

        Mock<ISourceProvider> mockSourceProvider = new();
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
        PackageId pkgRootId = new("github.com/test/root", string.Empty);
        SemVersion pkgRootVer = new(1, 0, 0);
        PackageSpec pkgRootSpec = new(pkgRootId, pkgRootVer);

        PackageId pkgDepId = new("github.com/test/dep", string.Empty);
        SemVersion pkgDepVer = new(2, 0, 0);
        PackageSpec pkgDepSpec = new(pkgDepId, pkgDepVer);

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
        Mock<ISourceProvider> rootProvider = new();
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
        Mock<ISourceProvider> depProvider = new();
        depProvider.Setup(p => p.OpenRead("tooth.json"))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(depManifestJson))));

        _mockSourceService.Setup(s => s.Get(pkgRootSpec)).ReturnsAsync(rootProvider.Object);
        _mockSourceService.Setup(s => s.Get(pkgDepSpec)).ReturnsAsync(depProvider.Object);

        PackageManifest depManifest = new()
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
        PackageSpec pkgSpec = new(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));
        Mock<ISourceProvider> mockSourceProvider = new();

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
        PackageId pkgId = new("github.com/test/pkg", "");
        SemVersion pkgVersion = new(1, 0, 0);
        PackageSpec pkgSpec = new(pkgId, pkgVersion);

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
        PackageId rootId = new("github.com/test/root", "");
        PackageId depId = new("github.com/test/dep", "");

        PackageSpec rootSpec = new(rootId, new SemVersion(1, 0, 0));
        PackageSpec depSpec = new(depId, new SemVersion(1, 0, 0));

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
        PackageId pkgId = new("github.com/test/pkg", "");
        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit))
            .ReturnsAsync([]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UninstallPackages([pkgId], false, false, false));
    }

    [Fact]
    public async Task InstallPackages_FlexibleVersion_ResolvesLatest()
    {
        PackageId pkgId = new("github.com/flexible/pkg", "");
        SemVersion v1 = new(1, 0, 0);
        SemVersion v2 = new(2, 0, 0);
        PackageSpec pkgSpecV2 = new(pkgId, v2);

        _mockPackageRegistry.Setup(r => r.GetAvailableVersions(pkgId))
            .ReturnsAsync(new[] { v1, v2 }.Order(SemVersion.PrecedenceComparer));

        Mock<ISourceProvider> mockSourceProvider = new();
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
        PackageSpec pkgSpec = new(new PackageId("github.com/test/pkg", ""), new SemVersion(1));
        Mock<ISourceProvider> mockSourceProvider = new();
        _mockSourceService.Setup(s => s.Get(pkgSpec)).ReturnsAsync(mockSourceProvider.Object);

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit))
            .ReturnsAsync([pkgSpec]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InstallPackages([pkgSpec], [], [], [], false, false, false));
    }

    [Fact]
    public async Task InstallPackages_DuplicateIds_ThrowsException()
    {
        PackageSpec pkgSpec1 = new(new PackageId("github.com/test/pkg", ""), new SemVersion(1));
        PackageSpec pkgSpec2 = new(new PackageId("github.com/test/pkg", ""), new SemVersion(2));

        Mock<ISourceProvider> mockSourceProvider = new();
        _mockSourceService.Setup(s => s.Get(It.IsAny<PackageSpec>())).ReturnsAsync(mockSourceProvider.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InstallPackages([pkgSpec1, pkgSpec2], [], [], [], false, false, true)); // noDependencies=true to skip solver
    }

    [Fact]
    public async Task UpdatePackages_UpdatesExistingPackage()
    {
        PackageId pkgId = new("github.com/test/pkg", "");
        SemVersion oldVer = new(1);
        SemVersion newVer = new(2);

        PackageSpec oldSpec = new(pkgId, oldVer);
        PackageSpec newSpec = new(pkgId, newVer);

        // Setup installed state
        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([oldSpec]);
        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit))
            .ReturnsAsync([oldSpec]);

        // Setup new package resolution
        Mock<ISourceProvider> mockSourceProvider = new();
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
        PackageSpec pkgSpec = new(new PackageId("github.com/test/pkg", ""), new SemVersion(2));
        Mock<ISourceProvider> mockSourceProvider = new();
        _mockSourceService.Setup(s => s.Get(pkgSpec)).ReturnsAsync(mockSourceProvider.Object);

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([]); // Nothing installed

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdatePackages([pkgSpec], [], [], [], false, false));
    }
}