using Lip.Context;
using Moq;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Tests;

public class PackageManagerTests
{
    private static readonly string s_cacheDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "cache")
        : Path.Join("/", "path", "to", "cache");

    private static readonly string s_workingDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "working")
        : Path.Join("/", "path", "to", "working");

    [Fact]
    public async Task GetCurrentPackageLock_NotFound_ReturnsDefault()
    {
        // Arrange.
        var expectedPackageLock = new PackageLock()
        {
            FormatVersion = PackageLock.DefaultFormatVersion,
            FormatUuid = PackageLock.DefaultFormatUuid,
            Locks = []
        };

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), s_workingDir);

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, []);

        // Act.
        var result = await packageManager.GetCurrentPackageLock();

        // Assert.
        Assert.Equal(expectedPackageLock.ToJsonBytes(), result.ToJsonBytes());
    }

    [Fact]
    public async Task GetCurrentPackageLock_Found_ReturnsPackageLock()
    {
        // Arrange.
        var expectedPackageLock = new PackageLock()
        {
            FormatVersion = PackageLock.DefaultFormatVersion,
            FormatUuid = PackageLock.DefaultFormatUuid,
            Locks = [
                new PackageLock.LockType()
                {
                    Locked = true,
                    Package = new PackageManifest()
                    {
                        FormatVersion = PackageManifest.DefaultFormatVersion,
                        FormatUuid = PackageManifest.DefaultFormatUuid,
                        ToothPath = "example.com/pkg",
                        VersionText = "1.0.0",
                    },
                    VariantLabel = "variant"
                }
            ]
        };

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { Path.Join(s_workingDir, "tooth_lock.json"), new MockFileData(expectedPackageLock.ToJsonBytes()) }
        }, s_workingDir);

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, []);

        // Act.
        var result = await packageManager.GetCurrentPackageLock();

        // Assert.
        Assert.Equal(expectedPackageLock.ToJsonBytes(), result.ToJsonBytes());
    }
}
