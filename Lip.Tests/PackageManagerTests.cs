using Flurl;
using Lip.Context;
using Microsoft.Extensions.Logging;
using Moq;
using Semver;
using System.IO.Abstractions.TestingHelpers;
using static Lip.Context.IGit;

namespace Lip.Tests;

public class PackageManagerTests
{
    private static readonly string s_cacheDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "cache")
        : Path.Join("/", "path", "to", "cache");

    private static readonly string s_workingDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "working")
        : Path.Join("/", "path", "to", "working");

    private static readonly PackageManifest s_examplePackage_1 = new()
    {
        FormatVersion = PackageManifest.DefaultFormatVersion,
        FormatUuid = PackageManifest.DefaultFormatUuid,
        ToothPath = "example.com/pkg",
        VersionText = "1.0.0",
    };

    private static readonly PackageLock s_examplePackageLock = new()
    {
        FormatVersion = PackageLock.DefaultFormatVersion,
        FormatUuid = PackageLock.DefaultFormatUuid,
        Locks = [
                new PackageLock.LockType()
                {
                    Locked = true,
                    Package = s_examplePackage_1,
                    VariantLabel = "variant"
                }
            ]
    };

    private static PackageManager PackageManagerFromFiles(IDictionary<string, MockFileData> files)
    {
        var fileSystem = new MockFileSystem(files, s_workingDir);

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        return PackageManagerFromCxtAndFs(context, fileSystem);
    }

    private static PackageManager PackageManagerFromCxtAndFs(Mock<IContext> context, MockFileSystem fileSystem)
    {
        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        return new PackageManager(context.Object, cacheManager, pathManager, []);
    }

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

        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData>());

        // Act.
        var result = await packageManager.GetCurrentPackageLock();

        // Assert.
        Assert.Equal(expectedPackageLock.ToJsonBytes(), result.ToJsonBytes());
    }

    [Fact]
    public async Task GetCurrentPackageLock_Found_ReturnsPackageLock()
    {
        // Arrange.
        var expectedPackageLock = s_examplePackageLock;

        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData>
        {
            { Path.Join(s_workingDir, "tooth_lock.json"), new MockFileData(expectedPackageLock.ToJsonBytes()) }
        });

        // Act.
        var result = await packageManager.GetCurrentPackageLock();

        // Assert.
        Assert.Equal(expectedPackageLock.ToJsonBytes(), result.ToJsonBytes());
    }

    [Fact]
    public async Task GetCurrentPackageManifestParsed_NotExits()
    {
        // Arrange.
        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData>());

        // Act.
        var result = await packageManager.GetCurrentPackageManifestWithTemplate();

        // Assert.
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentPackageManifestParsed_Exits()
    {
        // Arrange.
        var expectedPackage = s_examplePackage_1;

        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData>() {
            { Path.Join(s_workingDir, "tooth.json"), new MockFileData(expectedPackage.ToJsonBytes()) }
        });

        // Act.
        var result = await packageManager.GetCurrentPackageManifestWithTemplate();

        // Assert.
        Assert.Equal(expectedPackage, result);
    }

    [Fact]
    public async Task GetPackageManifestFromInstalledPackages_Found()
    {
        // Arrange.
        var expectedPackage = s_examplePackage_1;

        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData> {
            { Path.Join(s_workingDir, "tooth_lock.json"), new MockFileData(s_examplePackageLock.ToJsonBytes()) }
        });

        var specifier = new PackageSpecifierWithoutVersion { ToothPath = expectedPackage.ToothPath, VariantLabel = "variant" };

        // Act.
        var result = await packageManager.GetPackageManifestFromInstalledPackages(specifier);

        // Assert.
        Assert.Equal(expectedPackage, result);
    }

    [Fact]
    public async Task GetPackageManifestFromInstalledPackages_NotFound()
    {
        // Arrange.
        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData> {
            { Path.Join(s_workingDir, "tooth_lock.json"), new MockFileData(s_examplePackageLock.ToJsonBytes()) }
        });

        var specifier = new PackageSpecifierWithoutVersion { ToothPath = "example.com/pkg1", VariantLabel = "variant" };

        // Act.
        var result = await packageManager.GetPackageManifestFromInstalledPackages(specifier);

        // Assert.
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPackageManifestFromSpecifier_Found()
    {
        // Arrange.
        var expectedPackage = s_examplePackage_1;

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            {
                Path.Join(s_cacheDir, "git_repos", "https%3A%2F%2Fexample.com%2Fpkg", "v1.0.0", "tooth.json"),
                new MockFileData(expectedPackage.ToJsonBytes())
            },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(new Mock<IGit>().Object);

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        var result = await packageManager.GetPackageManifestFromSpecifier(new PackageSpecifier
        {
            ToothPath = expectedPackage.ToothPath,
            VariantLabel = "",
            Version = expectedPackage.Version
        });

        // Assert.
        Assert.Equal(expectedPackage, result);
    }

    [Fact]
    public async Task GetPackageManifestFromSpecifier_NotFound()
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(new Mock<IGit>().Object);

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        var result = await packageManager.GetPackageManifestFromSpecifier(new PackageSpecifier
        {
            ToothPath = s_examplePackage_1.ToothPath,
            VariantLabel = "",
            Version = s_examplePackage_1.Version
        });

        // Assert.
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPackageManifestFromFileSource_Found()
    {
        // Arrange.
        var expectedPackage = s_examplePackage_1;

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            { Path.Join(s_workingDir, "tooth.json"),new MockFileData(expectedPackage.ToJsonBytes())}
        }, s_workingDir);

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var fileSource = new DirectoryFileSource(fileSystem, s_workingDir);

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        var result = await packageManager.GetPackageManifestFromFileSource(fileSource);

        // Assert.
        Assert.Equal(expectedPackage, result);
    }

    [Fact]
    public async Task GetPackageManifestFromFileSource_NotFound()
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), s_workingDir);

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var fileSource = new DirectoryFileSource(fileSystem, s_workingDir);

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        var result = await packageManager.GetPackageManifestFromFileSource(fileSource);

        // Assert.
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPackageRemoteVersions_WithGoModuleProxy()
    {
        // Arrange.
        var expectedVersions = new List<SemVersion> { new(0, 1, 0), new(0, 2, 0), new(0, 3, 0) };
        var versionFile = string.Join("\n", expectedVersions.Select((ver) => "v" + ver.ToString())) + "\n0.4.0";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), s_workingDir);

        var downloader = new Mock<IDownloader>();
        downloader.Setup(d => d.DownloadFile(Url.Parse("https://example.com/example.com/user/repo/@v/list"), It.IsAny<string>()))
        .Callback<Url, string>((_, destinationPath) =>
        {
            fileSystem.AddFile(destinationPath, new MockFileData(versionFile));
        });

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Downloader).Returns(downloader.Object);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, [Url.Parse("https://example.com")]);

        // Act.
        var result = await packageManager.GetPackageRemoteVersions(new PackageSpecifierWithoutVersion
        {
            ToothPath = "example.com/user/repo",
            VariantLabel = ""
        });

        // Assert.
        Assert.Equal(expectedVersions, result);
    }

    [Fact]
    public async Task GetPackageRemoteVersions_WithGit()
    {
        // Arrange.
        var expectedVersions = new List<SemVersion> { new(0, 1, 0), new(0, 2, 0), new(0, 3, 0) };

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), s_workingDir);

        var git = new Mock<IGit>();
        git.Setup(g => g.ListRemote("https://example.com/user/repo",true,true))
        .Returns(
            Task<List<ListRemoteResultItem>>.Factory.StartNew(() => [
                new ListRemoteResultItem{Sha ="175394eb04c96bd99dc095bbbd337008a9cbffa1" ,Ref = "refs/tags/v0.1.0"},
                new ListRemoteResultItem{Sha ="ef73ef6d1aadb96355f13cba845a79727cc52ddd" ,Ref = "refs/tags/v0.2.0"},
                new ListRemoteResultItem{Sha ="278d385619bbc5191eb326fee5f89fe6af2b1031" ,Ref = "refs/tags/v0.3.0"},
                new ListRemoteResultItem{Sha ="a9e0f95779dcaa218d763a4278813f2298305f07" ,Ref = "refs/pull/101/head"},
                new ListRemoteResultItem{Sha ="66fdb3a16edbcc48e7de49f9b786f38680116477" ,Ref = "refs/heads/feat/schema-v3"}
            ])
        );

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(git.Object);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, []);

        // Act.
        var result = await packageManager.GetPackageRemoteVersions(new PackageSpecifierWithoutVersion
        {
            ToothPath = "example.com/user/repo",
            VariantLabel = ""
        });

        // Assert.
        Assert.Equal(expectedVersions, result);
    }

    [Fact]
    public async Task GetPackageRemoteVersions_WithGoModuleProxyFailed_And_NoGit()
    {
        // Arrange.
        var packageSpecifier = new PackageSpecifierWithoutVersion
            {
                ToothPath = "example.com/user/repo",
                VariantLabel = ""
            };

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), s_workingDir);

        var downloader = new Mock<IDownloader>();
        downloader.Setup(d => d.DownloadFile(Url.Parse("https://example.com/example.com/user/repo/@v/list"), It.IsAny<string>()))
        .Callback<Url, string>((_, _) =>
        {
            throw new Exception("DownLoad FAIL");
        });

        var logger = new Mock<ILogger>();

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Downloader).Returns(downloader.Object);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, [Url.Parse("https://example.com")]);

        // Act. & Assert.
        await Assert.ThrowsAnyAsync<InvalidOperationException>(async () =>
        {
            var result = await packageManager.GetPackageRemoteVersions(packageSpecifier);
        });
    }
}
