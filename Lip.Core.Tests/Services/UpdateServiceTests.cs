using Flurl;
using Lip.Core.Context;
using Lip.Core.PackageRegistries;
using Lip.Core.Services; // Ensure Services for InstallService/UpdateService
using Microsoft.Extensions.Logging;
using Moq;
using Semver;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;

namespace Lip.Core.Tests;

using static Lip.Core.PackageLock;

public class UpdateServiceTests
{
    private readonly Mock<ICacheManager> _cacheManagerMock = new();
    private readonly Mock<IContext> _contextMock = new();
    private readonly Mock<IDependencySolver> _dependencySolverMock = new();
    private readonly Mock<IPackageManager> _packageManagerMock = new();
    private readonly Mock<IPackageRegistry> _packageRegistryMock = new();
    private readonly Mock<IPathManager> _pathManagerMock = new();
    private readonly RuntimeConfig _runtimeConfig = new();
    private readonly MockFileSystem _fileSystem = new();
    private readonly Mock<ILogger> _loggerMock = new();

    private readonly string _workingDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\app" : "/app";
    private readonly UpdateService _updateService;

    public UpdateServiceTests()
    {
        _contextMock.Setup(c => c.FileSystem).Returns(_fileSystem);
        _contextMock.Setup(c => c.Logger).Returns(_loggerMock.Object);
        _pathManagerMock.Setup(p => p.WorkingDir).Returns(_workingDir);

        var installService = new InstallService(
            _contextMock.Object,
            _packageManagerMock.Object,
            _dependencySolverMock.Object,
            _cacheManagerMock.Object,
            _packageRegistryMock.Object,
            _pathManagerMock.Object);

        _updateService = new UpdateService(
            _contextMock.Object,
            _packageManagerMock.Object,
            installService);
    }

    private PackageManifest CreateManifest(string name, string version)
    {
        return new PackageManifest
        {
            FormatVersion = DefaultFormatVersion,
            FormatUuid = DefaultFormatUuid,
            ToothPath = $"github.com/test/{name}",
            Version = SemVersion.Parse(version),
            Info = new PackageManifest.InfoType
            {
                Name = name,
                Description = "Test package",
                Tags = [],
                AvatarUrl = new Url("http://example.com/avatar")
            },
            Variants =
            [
                new PackageManifest.Variant
                {
                    Label = "",
                    Platform = RuntimeInformation.RuntimeIdentifier,
                    Dependencies = [],
                    Assets = [],
                    PreserveFiles = [],
                    RemoveFiles = [],
                    Scripts = new PackageManifest.ScriptsType
                    {
                        PreInstall = [],
                        Install = [],
                        PostInstall = [],
                        PrePack = [],
                        PostPack = [],
                        PreUninstall = [],
                        Uninstall = [],
                        PostUninstall = [],

                    }
                }
            ]
        };
    }

    private PackageLock.Package CreateLockedPackage(string name, string version)
    {
        var manifest = CreateManifest(name, version);
        return new PackageLock.Package
        {
            Locked = true,
            Manifest = manifest,
            VariantLabel = "",
            Files = []
        };
    }

    [Fact]
    public async Task Update_CallsInstallWithUpgradeLockedPackagesTrue()
    {
        // Arrange
        var userInput = new List<string> { "github.com/test/pkg-update" };

        // We use a locked package to verify it requests AtLeast version (which implies UpgradeLockedPackages=true)
        var lockedPackage = CreateLockedPackage("locked-pkg", "1.0.0");
        var pkgUpdateLocked = CreateLockedPackage("pkg-update", "1.0.0"); // Update package is also installed

        _packageManagerMock.Setup(pm => pm.GetCurrentPackageLock())
            .ReturnsAsync(new PackageLock { Packages = [lockedPackage, pkgUpdateLocked] });
        _packageManagerMock.Setup(pm => pm.GetPackageFromLock(It.Is<PackageIdentifier>(id => id.ToString() == "github.com/test/locked-pkg")))
             .ReturnsAsync(lockedPackage);
        _packageManagerMock.Setup(pm => pm.GetPackageFromLock(It.Is<PackageIdentifier>(id => id.ToString() == "github.com/test/pkg-update")))
             .ReturnsAsync(pkgUpdateLocked);

        // Mock user input resolution
        _packageRegistryMock.Setup(pm => pm.GetVersions(It.IsAny<PackageIdentifier>()))
            .ReturnsAsync(new List<SemVersion> { SemVersion.Parse("2.0.0") });
        _cacheManagerMock.Setup(cm => cm.GetPackageFileSource(It.IsAny<PackageSpecifier>()))
            .ReturnsAsync(new Mock<IFileSource>().Object);
        _packageManagerMock.Setup(pm => pm.GetPackageManifestFromFileSource(It.IsAny<IFileSource>()))
            .ReturnsAsync(CreateManifest("pkg-update", "2.0.0"));

        // Mock dependency resolution success (return empty list)
        _dependencySolverMock.Setup(ds => ds.ResolveDependencies(It.IsAny<IEnumerable<(PackageIdentifier, SemVersionRange)>>(), It.IsAny<IEnumerable<PackageLock.Package>>()))
            .ReturnsAsync(new List<PackageSpecifier>());

        // Act
        await _updateService.Update(userInput);

        // Assert
        // Verify dependency solver was called with AtLeast range for the locked package
        // This confirms that UpgradeLockedPackages was true inside Install
        _dependencySolverMock.Verify(ds => ds.ResolveDependencies(
            It.Is<IEnumerable<(PackageIdentifier, SemVersionRange)>>(reqs =>
                reqs.Any(r => r.Item1.ToString() == "github.com/test/locked-pkg" && r.Item2.ToString() == ">=1.0.0")
            ),
            It.IsAny<IEnumerable<PackageLock.Package>>()
        ), Times.Once);
    }
    [Fact]
    public async Task Update_WithUninstalledPackage_LogsWarningAndDoesNotInstall()
    {
        // Arrange
        var userInput = new List<string> { "github.com/test/uninstalled-pkg" };
        // Act
        await _updateService.Update(userInput);

        // Assert
        // Verify ResolveDependencies was NEVER called (since Install should be skipped)
        _dependencySolverMock.Verify(ds => ds.ResolveDependencies(
            It.IsAny<IEnumerable<(PackageIdentifier, SemVersionRange)>>(),
            It.IsAny<IEnumerable<PackageLock.Package>>()
        ), Times.Never);

        // Verify warning was logged
        _loggerMock.Verify(static l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>(static (v, t) => v.ToString()!.Contains("is not installed. Skipping.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}