using Lip.Core.Entities;
using Lip.Core.Infrastructure;

using Lip.Core.Services;
using Lip.Core.SourceProviders;
using Moq;
using Semver;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.Services;

public class PackageInstallerTests
{
    private readonly Mock<ICommandRunner> _mockCommandRunner;
    private readonly MockFileSystem _mockFileSystem;
    private readonly Mock<IUserInteraction> _userInteraction;
    private readonly Mock<ISourceService> _mockSourceService;
    private readonly Mock<IWorkspaceService> _mockWorkspaceService;
    private readonly PackageInstaller _installer;

    public PackageInstallerTests()
    {
        _mockCommandRunner = new Mock<ICommandRunner>();
        _mockFileSystem = new MockFileSystem();
        _userInteraction = new Mock<IUserInteraction>();
        _mockSourceService = new Mock<ISourceService>();
        _mockWorkspaceService = new Mock<IWorkspaceService>();

        _installer = new PackageInstaller(
            _mockCommandRunner.Object,
            _mockFileSystem,
            _userInteraction.Object,
            _mockSourceService.Object,
            _mockWorkspaceService.Object);
    }

    [Fact]
    public async Task InstallPackage_AlreadyInstalled_ThrowsException()
    {
        // Arrange
        PackageSpec pkgSpec = new(new PackageId("github.com/test/pkg", string.Empty), new SemVersion(1, 0, 0));
        PackageArtifact artifact = new(pkgSpec, new Mock<ISourceProvider>().Object);

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([pkgSpec]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _installer.InstallPackage(
            artifact,
            dryRun: false,
            explicitInstall: false,
            ignoreScripts: false));
    }

    [Fact]
    public async Task UninstallPackage_NotInstalled_ThrowsException()
    {
        // Arrange
        PackageId pkgId = new("github.com/test/pkg", string.Empty);

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _installer.UninstallPackage(
            pkgId,
            dryRun: false,
            ignoreScripts: false));
    }
    [Fact]
    public async Task InstallPackage_ValidArtifact_InstallsFiles()
    {
        // Arrange
        PackageId pkgId = new("github.com/test/pkg", "");
        SemVersion pkgVer = new(1, 0, 0);
        PackageSpec pkgSpec = new(pkgId, pkgVer);

        // Setup WorkspaceService to allow install
        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([]);

        // Setup Manifest
        string manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/test/pkg",
                "version": "1.0.0",
                "variants": [
                    {
                        "assets": [
                            {
                                "type": "self",
                                "placements": [
                                    { "type": "file", "src": "file.txt", "dest": "plugins/file.txt" }
                                ] 
                            }
                        ]
                    }
                ]
            }
            """;
        Mock<ISourceProvider> mockSourceProvider = new();
        mockSourceProvider.Setup(p => p.OpenRead("tooth.json"))
            .ReturnsAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(manifestJson)));

        mockSourceProvider.Setup(p => p.Keys).Returns(["file.txt"]);
        mockSourceProvider.Setup(p => p.OpenRead("file.txt"))
            .ReturnsAsync(new MemoryStream("content"u8.ToArray()));

        PackageArtifact artifact = new(pkgSpec, mockSourceProvider.Object);

        // Act
        await _installer.InstallPackage(artifact, false, false, false);

        // Assert
        Assert.True(_mockFileSystem.File.Exists(@"plugins\file.txt"));
        _mockWorkspaceService.Verify(w => w.AddInstalledPackage(pkgSpec, It.IsAny<PackageManifest>(), It.IsAny<IEnumerable<IFileInfo>>(), false), Times.Once);
    }

    [Fact]
    public async Task InstallPackage_RunsScripts()
    {
        // Arrange
        PackageId pkgId = new("github.com/test/pkg", "");
        PackageSpec pkgSpec = new(pkgId, new SemVersion(1, 0, 0));

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([]);

        string manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/test/pkg",
                "version": "1.0.0",
                "variants": [
                    {
                        "scripts": {
                            "pre_install": ["echo pre"],
                            "post_install": ["echo post"]
                        }
                    }
                ]
            }
            """;

        Mock<ISourceProvider> mockSourceProvider = new();
        mockSourceProvider.Setup(p => p.OpenRead("tooth.json"))
            .ReturnsAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(manifestJson)));
        mockSourceProvider.Setup(p => p.Keys).Returns([]);

        PackageArtifact artifact = new(pkgSpec, mockSourceProvider.Object);

        // Act
        await _installer.InstallPackage(artifact, false, false, false);

        // Assert
        _mockCommandRunner.Verify(c => c.Run("echo pre"), Times.Once);
        _mockCommandRunner.Verify(c => c.Run("echo post"), Times.Once);
    }

    [Fact]
    public async Task UninstallPackage_RemovesFiles()
    {
        // Arrange
        PackageId pkgId = new("github.com/test/pkg", "");
        PackageSpec pkgSpec = new(pkgId, new SemVersion(1, 0, 0));

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([pkgSpec]);

        PackageManifest manifest = new()
        {
            Path = "github.com/test/pkg",
            Version = new SemVersion(1, 0, 0),
            Variants = [new()]
        };
        _mockWorkspaceService.Setup(w => w.GetInstalledPackageManifest(pkgSpec))
            .ReturnsAsync(manifest);

        string filePath = @"plugins\file.txt";
        _mockFileSystem.AddFile(filePath, new MockFileData("content"));

        _mockWorkspaceService.Setup(w => w.GetInstalledPackageFiles(pkgSpec))
            .ReturnsAsync([_mockFileSystem.FileInfo.New(filePath)]);

        // Act
        await _installer.UninstallPackage(pkgId, false, false);

        // Assert
        Assert.False(_mockFileSystem.File.Exists(filePath));
        _mockWorkspaceService.Verify(w => w.RemoveInstalledPackage(pkgSpec), Times.Once);
    }

    [Fact]
    public async Task UninstallPackage_PreserveFiles_KeepsFiles()
    {
        // Arrange
        PackageId pkgId = new("github.com/test/pkg", "");
        PackageSpec pkgSpec = new(pkgId, new SemVersion(1, 0, 0));

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([pkgSpec]);

        PackageManifest manifest = new()
        {
            Path = "github.com/test/pkg",
            Version = new SemVersion(1, 0, 0),
            Variants = [
                new() {
                    PreserveFiles = [DotNet.Globbing.Glob.Parse("*.txt")]
                }
            ]
        };
        _mockWorkspaceService.Setup(w => w.GetInstalledPackageManifest(pkgSpec))
            .ReturnsAsync(manifest);

        string filePath = @"plugins\file.txt";
        _mockFileSystem.AddFile(filePath, new MockFileData("content"));

        _mockWorkspaceService.Setup(w => w.GetInstalledPackageFiles(pkgSpec))
            .ReturnsAsync([_mockFileSystem.FileInfo.New(filePath)]);

        // Act
        await _installer.UninstallPackage(pkgId, false, false);

        // Assert
        Assert.True(_mockFileSystem.File.Exists(filePath)); // Should exist
    }

    [Fact]
    public async Task UninstallPackage_RemoveFiles_RemovesExtraFiles()
    {
        // Arrange
        PackageId pkgId = new("github.com/test/pkg", "");
        PackageSpec pkgSpec = new(pkgId, new SemVersion(1, 0, 0));

        _mockWorkspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .ReturnsAsync([pkgSpec]);

        PackageManifest manifest = new()
        {
            Path = "github.com/test/pkg",
            Version = new SemVersion(1, 0, 0),
            Variants = [
                new() {
                    RemoveFiles = [DotNet.Globbing.Glob.Parse("extra.log")]
                }
            ]
        };
        _mockWorkspaceService.Setup(w => w.GetInstalledPackageManifest(pkgSpec))
            .ReturnsAsync(manifest);
        _mockWorkspaceService.Setup(w => w.GetInstalledPackageFiles(pkgSpec))
            .ReturnsAsync([]);

        string extraFile = "extra.log";
        _mockFileSystem.AddFile(extraFile, new MockFileData("log"));

        // Act
        await _installer.UninstallPackage(pkgId, false, false);

        // Assert
        Assert.False(_mockFileSystem.File.Exists(extraFile));
    }
}