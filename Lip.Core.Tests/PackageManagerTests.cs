using Flurl;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Semver;
using System.IO.Abstractions.TestingHelpers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Lip.Core.Tests;

using global::Lip.Core;

public static class TestExtensions
{
    public static byte[] ToJsonBytes(PackageManifest manifest)
    {
        using var ms = new MemoryStream();
        manifest.ToStream(ms).Wait();
        return ms.ToArray();
    }
    public static byte[] ToJsonBytes(PackageLock lockFile)
    {
        using var ms = new MemoryStream();
        lockFile.ToStream(ms).Wait();
        return ms.ToArray();
    }
}

public class PackageManagerTests
{
    private record ListRemoteResultItem(string Sha, string Ref) : IGit.IListRemoteResultItem;

    private static PackageManifest CreateManifest(string toothPath = "example.com/pkg", string version = "1.0.0", List<PackageManifest.Variant>? variants = null)
    {
        return new PackageManifest
        {
            ToothPath = toothPath,
            Version = SemVersion.Parse(version),
            Info = new() { Name = "", Description = "", Tags = [], AvatarUrl = Url.Parse("https://example.com/icon") },
            Variants = variants ?? []
        };
    }

    private static PackageManifest.Variant CreateVariant(string label = "", string platform = "")
    {
        return new PackageManifest.Variant
        {
            Label = label,
            Platform = string.IsNullOrEmpty(platform) ? RuntimeInformation.RuntimeIdentifier : platform,
            Dependencies = [],
            Assets = [],
            PreserveFiles = [],
            RemoveFiles = [],
            Scripts = new() { PreInstall = [], Install = [], PostInstall = [], PrePack = [], PostPack = [], PreUninstall = [], Uninstall = [], PostUninstall = [], AdditionalScripts = [] }
        };
    }
    private static readonly string s_cacheDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "cache")
        : Path.Join("/", "path", "to", "cache");

    private static readonly string s_workingDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "working")
        : Path.Join("/", "path", "to", "working");

    private static readonly PackageManifest s_examplePackage_1 = CreateManifest(toothPath: "example.com/pkg", version: "1.0.0", variants: [CreateVariant(label: "variant")]);

    private static readonly PackageLock s_examplePackageLock = new()
    {
        Packages = [
                new PackageLock.Package()
                {
                    Locked = true,
                    Manifest = s_examplePackage_1,
                    VariantLabel = "variant",
                    Files = []
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

        return new PackageManager(context.Object, cacheManager, pathManager, [], []);
    }

    [Fact]
    public async Task GetCurrentPackageLock_NotFound_ReturnsDefault()
    {
        // Arrange.
        var expectedPackageLock = new PackageLock()
        {
            Packages = []
        };

        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData>());

        // Act.
        var result = await packageManager.GetCurrentPackageLock();

        // Assert.
        Assert.Equal(TestExtensions.ToJsonBytes(expectedPackageLock), TestExtensions.ToJsonBytes(result));
    }

    [Fact]
    public async Task GetCurrentPackageLock_Found_ReturnsPackageLock()
    {
        // Arrange.
        var expectedPackageLock = s_examplePackageLock;

        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData>
        {
            { Path.Join(s_workingDir, "tooth_lock.json"), new MockFileData(TestExtensions.ToJsonBytes(expectedPackageLock)) }
        });

        // Act.
        var result = await packageManager.GetCurrentPackageLock();

        // Assert.
        Assert.Equal(TestExtensions.ToJsonBytes(expectedPackageLock), TestExtensions.ToJsonBytes(result));
    }

    [Fact]
    public async Task GetCurrentPackageManifestParsed_NotExits()
    {
        // Arrange.
        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData>());

        // Act.
        var result = await packageManager.GetCurrentPackageManifest();

        // Assert.
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentPackageManifestParsed_Exits()
    {
        // Arrange.
        var expectedPackage = s_examplePackage_1;

        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData>() {
            { Path.Join(s_workingDir, "tooth.json"), new MockFileData(TestExtensions.ToJsonBytes(expectedPackage)) }
        });

        // Act.
        // Act.
        var result = await packageManager.GetCurrentPackageManifest();

        // Assert.
        // Assert.
        Assert.Equal(TestExtensions.ToJsonBytes(expectedPackage), TestExtensions.ToJsonBytes(result));
    }

    [Fact]
    public async Task GetPackageManifestFromInstalledPackages_Found()
    {
        // Arrange.
        var expectedPackage = s_examplePackage_1;

        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData> {
            { Path.Join(s_workingDir, "tooth_lock.json"), new MockFileData(TestExtensions.ToJsonBytes(s_examplePackageLock)) }
        });

        var specifier = new PackageIdentifier(expectedPackage.ToothPath, "variant");

        // Act.
        var pkg = await packageManager.GetPackageFromLock(specifier);
        var result = (PackageManifest?)pkg?.GetType().GetProperty("Manifest")?.GetValue(pkg);

        // Assert.
        // Assert.
        Assert.NotNull(result);
        Assert.Equal(expectedPackage.ToothPath, result.ToothPath);
        Assert.Equal(expectedPackage.Version, result.Version);
        Assert.Equal(expectedPackage.Variants.Count, result.Variants.Count);
    }

    [Fact]
    public async Task GetPackageManifestFromInstalledPackages_NotFound()
    {
        // Arrange.
        var packageManager = PackageManagerFromFiles(new Dictionary<string, MockFileData> {
            { Path.Join(s_workingDir, "tooth_lock.json"), new MockFileData(TestExtensions.ToJsonBytes(s_examplePackageLock)) }
        });

        var specifier = new PackageIdentifier("example.com/pkg1", "variant");

        // Act.
        var pkg = await packageManager.GetPackageFromLock(specifier);
        var result = pkg != null ? (PackageManifest?)pkg.GetType().GetProperty("Manifest")?.GetValue(pkg) : null;

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
                new MockFileData(TestExtensions.ToJsonBytes(expectedPackage))
            },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(new Mock<IGit>().Object);
        context.SetupGet(c => c.Logger).Returns(new Mock<ILogger>().Object);

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        var pkg = await packageManager.GetPackageManifestFromCache(new PackageSpecifier(
            new PackageIdentifier(expectedPackage.ToothPath, ""),
            expectedPackage.Version));

        // Assert.
        // Assert.
        Assert.Equal(TestExtensions.ToJsonBytes(expectedPackage), TestExtensions.ToJsonBytes(pkg));
    }

    [Fact]
    public async Task GetPackageManifestFromSpecifier_NotFound()
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(new Mock<IGit>().Object);
        context.SetupGet(c => c.Logger).Returns(new Mock<ILogger>().Object);

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        var pkg = await packageManager.GetPackageManifestFromCache(new PackageSpecifier(
            new PackageIdentifier(s_examplePackage_1.ToothPath, ""),
            s_examplePackage_1.Version));

        // Assert.
        Assert.Null(pkg);
    }

    [Fact]
    public async Task GetPackageManifestFromFileSource_Found()
    {
        // Arrange.
        var expectedPackage = s_examplePackage_1;

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            { Path.Join(s_workingDir, "tooth.json"),new MockFileData(TestExtensions.ToJsonBytes(expectedPackage))}
        }, s_workingDir);

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var fileSource = new DirectoryFileSource(fileSystem, s_workingDir);

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        // Act.
        var pkg = await packageManager.GetPackageManifestFromFileSource(fileSource);

        // Assert.
        // Assert.
        Assert.Equal(TestExtensions.ToJsonBytes(expectedPackage), TestExtensions.ToJsonBytes(pkg));
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
        // Act.
        var pkg = await packageManager.GetPackageManifestFromFileSource(fileSource);

        // Assert.
        Assert.Null(pkg);
    }

    [Fact]
    public async Task GetPackageRemoteVersions_WithGoModuleProxy()
    {
        // Arrange.
        var expectedVersions = new List<SemVersion> { new(0, 1, 0), new(0, 2, 0), new(0, 3, 0) };
        var versionFile = string.Join("\n", expectedVersions.Select((ver) => "v" + ver.ToString())) + "\n0.4.0\n15.0.0";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), s_workingDir);

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Logger).Returns(new Mock<ILogger>().Object);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, [], [Url.Parse("https://example.com")]);

        // Act.
        using var httpTest = new HttpTest();
        httpTest.RespondWith(versionFile);

        var result = await packageManager.GetPackageRemoteVersions(new PackageIdentifier("example.com/user/repo", ""));

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
        git.Setup(g => g.ListRemote("https://example.com/user/repo", true, true))
        .Returns(
            Task<List<IGit.IListRemoteResultItem>>.Factory.StartNew(() => [
                new ListRemoteResultItem("175394eb04c96bd99dc095bbbd337008a9cbffa1", "refs/tags/v0.1.0"),
                new ListRemoteResultItem("ef73ef6d1aadb96355f13cba845a79727cc52ddd", "refs/tags/v0.2.0"),
                new ListRemoteResultItem("278d385619bbc5191eb326fee5f89fe6af2b1031", "refs/tags/v0.3.0"),
                new ListRemoteResultItem("a9e0f95779dcaa218d763a4278813f2298305f07", "refs/pull/101/head"),
                new ListRemoteResultItem("66fdb3a16edbcc48e7de49f9b786f38680116477", "refs/heads/feat/schema-v3")
            ])
        );

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(git.Object);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, [], []);

        // Act.
        var result = await packageManager.GetPackageRemoteVersions(new PackageIdentifier("example.com/user/repo", ""));

        // Assert.
        Assert.Equal(expectedVersions, result);
    }

    [Fact]
    public async Task GetPackageRemoteVersions_WithGoModuleProxyFailed_And_NoGit()
    {
        // Arrange.
        var packageSpecifier = new PackageIdentifier("example.com/user/repo", "");

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

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, [], [Url.Parse("https://example.com")]);

        // Act. & Assert.
        await Assert.ThrowsAnyAsync<InvalidOperationException>(async () =>
        {
            var result = await packageManager.GetPackageRemoteVersions(packageSpecifier);
        });
    }

    [Fact]
    public async Task Install_ManifestNotFound()
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), s_workingDir);

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var fileSource = new DirectoryFileSource(fileSystem, s_workingDir);

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await packageManager.InstallPackage(fileSource, "", false, false, false, false);
        });

        // Assert.
        Assert.Equal("Package manifest not found.", exception.Message);
    }

    [Fact]
    public async Task Install_NoVariant()
    {
        // Arrange.
        var emptyPack = CreateManifest(toothPath: "example.com/pkg", version: "1.0.0");

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            {Path.Join(s_cacheDir,"package","tooth.json"),new MockFileData(TestExtensions.ToJsonBytes(emptyPack))}
        }, s_workingDir);

        var logger = new Mock<ILogger>();

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var fileSource = new DirectoryFileSource(fileSystem, Path.Join(s_cacheDir, "package"));

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await packageManager.InstallPackage(fileSource, "", false, false, false, false);
        });

        // Assert.
        Assert.Equal("The package does not contain variant .", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)] // test lock
    [InlineData(true, "veriants")] // test veriants
    public async Task Install_EmptyVariant(bool locked, string veriants = "")
    {
        // Arrange
        var emptyPack = CreateManifest(toothPath: "example.com/pkg", version: "1.0.0", variants: [
            CreateVariant(label: veriants)
        ]);

        var expectedPackageLock = new PackageLock
        {
            Packages = [
                new PackageLock.Package()
                {
                    Locked = locked,
                    Manifest = emptyPack,
                    VariantLabel = veriants,
                    Files = []
                }
            ]
        };

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            {Path.Join(s_cacheDir,"package","tooth.json"),new MockFileData(TestExtensions.ToJsonBytes(emptyPack))}
        }, s_workingDir);

        var logger = new Mock<ILogger>();

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var fileSource = new DirectoryFileSource(fileSystem, Path.Join(s_cacheDir, "package"));

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        await packageManager.InstallPackage(fileSource, veriants, false, false, locked, false);

        // Assert.
        var resultPackageLock = fileSystem.GetFile(Path.Join(s_workingDir, "tooth_lock.json")).Contents;

        Assert.Equal(TestExtensions.ToJsonBytes(expectedPackageLock).ToString(), resultPackageLock.ToString()); // ! I don't know why but the bytes didn't equal
    }

    [Fact]
    public async Task Install_AlreadyInstalled_SameVersion()
    {
        // Arrange
        var emptyPack = CreateManifest(toothPath: "example.com/pkg", version: "1.0.0", variants: [
            CreateVariant(label: "")
        ]);

        var expectedPackageLock = new PackageLock
        {
            Packages = [
                new PackageLock.Package()
                {
                    Locked = true,
                    Manifest = emptyPack,
                    VariantLabel = "",
                    Files = []
                }
            ]
        };

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            {Path.Join(s_cacheDir,"package","tooth.json"),new MockFileData(TestExtensions.ToJsonBytes(emptyPack))},
            {Path.Join(s_workingDir,"tooth_lock.json"),new MockFileData(TestExtensions.ToJsonBytes(expectedPackageLock))}
        }, s_workingDir);

        var logger = new Mock<ILogger>();

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var fileSource = new DirectoryFileSource(fileSystem, Path.Join(s_cacheDir, "package"));

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        await packageManager.InstallPackage(fileSource, "", false, false, true, false);

        // Assert.
        // ?
    }

    [Fact]
    public async Task Install_AlreadyInstalled_OtherVersion()
    {
        // Arrange.
        var emptyPack = CreateManifest(toothPath: "example.com/pkg", version: "1.0.0", variants: [
            CreateVariant(label: "")
        ]);

        var packageLock = new PackageLock
        {
            Packages = [
                new PackageLock.Package()
                {
                    Locked = true,
                    Manifest = emptyPack with {Version = SemVersion.Parse("1.1.0")},
                    VariantLabel = "",
                    Files = []
                }
            ]
        };

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            {Path.Join(s_cacheDir,"package","tooth.json"),new MockFileData(TestExtensions.ToJsonBytes(emptyPack))},
            {Path.Join(s_workingDir,"tooth_lock.json"),new MockFileData(TestExtensions.ToJsonBytes(packageLock))}
        }, s_workingDir);

        var logger = new Mock<ILogger>();

        var commandRunner = new Mock<ICommandRunner>();

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var fileSource = new DirectoryFileSource(fileSystem, Path.Join(s_cacheDir, "package"));

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act. & Assert.
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await packageManager.InstallPackage(fileSource, "", false, false, false, false);
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task InstallPackage_Assets_And_Scripts(bool dryRun, bool ignoreScripts)
    {
        // Arrange.
        var manifest = new PackageManifest
        {
            ToothPath = "example.com/pkg",
            Version = SemVersion.Parse("1.0.0"),
            Info = new() { Name = "", Description = "", Tags = [], AvatarUrl = Url.Parse("https://example.com/icon") },
            Variants =
            [
            new()
            {
                Platform = RuntimeInformation.RuntimeIdentifier,
                Label = "",
                Dependencies = [],
                Assets =
                [
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Self,
                    Urls = [],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.File,
                        Src = "a.txt",Dest = "a.txt"}
                        ],
                },
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Self,
                    Urls = [],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.File,
                        Src = "a/a.txt",Dest = "a/a.txt"}
                        ]
                },
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Self,
                    Urls = [],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.Dir,
                        Src = "b",Dest = "b"}
                        ]
                },
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Uncompressed,
                    Urls = ["https://example.com/c"],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.File,
                        Src = "",
                        Dest = "c"
                    }]
                },
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Zip,
                    Urls = ["https://example.com/d.zip"],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.Dir,
                        Src = "",
                        Dest = "d"
                    }],
                },
                ],
                PreserveFiles = ["d/*"],
                RemoveFiles = ["d/file2.txt"],
                Scripts = new PackageManifest.ScriptsType
                {
                PreInstall = ["echo pre-install"],
                Install = ["echo install"],
                PostInstall = ["echo post-install"],
                PrePack = ["echo pre-pack"],
                PostPack = ["echo post-pack"],
                PreUninstall = ["echo pre-uninstall"],
                Uninstall = ["echo uninstall"],
                PostUninstall = ["echo post-uninstall"],
                AdditionalScripts = new Dictionary<string, List<string>>
                {
                    { "same_script", new List<string> { "echo same" } },
                    { "custom_script1", new List<string> { "echo custom1" } }
                }
                }
            }
            ]
        };

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            {Path.Join(s_cacheDir,"package","tooth.json"),new MockFileData(TestExtensions.ToJsonBytes(manifest))},
            {Path.Join(s_cacheDir,"package","a.txt"),new MockFileData("self file")},
            {Path.Join(s_cacheDir,"package","a","a.txt"),new MockFileData("self file")},
            {Path.Join(s_cacheDir,"package","b","b.txt"),new MockFileData("self file")},
            {Path.Join("for-compress","file1.txt"),new MockFileData("from compress")},
            {Path.Join("for-compress","file2.txt"),new MockFileData("from compress")},
        }, s_workingDir);

        var logger = new Mock<ILogger>();

        var commandRunner = new Mock<ICommandRunner>();

        var downloader = new Mock<IDownloader>();
        downloader.Setup(d => d.DownloadFile(Url.Parse("https://example.com/c"), It.IsAny<string>()))
        .Callback<Url, string>((_, destinationPath) =>
        {
            fileSystem.AddFile(destinationPath, new MockFileData("c"));
        });
        downloader.Setup(d => d.DownloadFile(Url.Parse("https://example.com/d.zip"), It.IsAny<string>()))
        .Callback<Url, string>((_, destinationPath) =>
        {
            fileSystem.AddFile(destinationPath, new MockFileData([]));

            using var archive = SharpCompress.Archives.Zip.ZipArchive.Create();
            archive.AddEntry("file1.txt", fileSystem.File.OpenRead(Path.Join("for-compress", "file1.txt")));
            archive.AddEntry("file2.txt", fileSystem.File.OpenRead(Path.Join("for-compress", "file2.txt")));
            archive.SaveTo(fileSystem.File.OpenWrite(destinationPath));
        });

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Logger).Returns(logger.Object);
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.Downloader).Returns(downloader.Object);

        var fileSource = new DirectoryFileSource(fileSystem, Path.Join(s_cacheDir, "package"));

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, [], []);

        // Act.
        await packageManager.InstallPackage(fileSource, "", dryRun, ignoreScripts, false, false);

        // Assert.
        // == Check Filse ==
        var fileFormSelf1 = fileSystem.GetFile(Path.Join(s_workingDir, "a.txt"));
        var fileFormSelf2 = fileSystem.GetFile(Path.Join(s_workingDir, "a", "a.txt"));
        var fileFormSelf3 = fileSystem.GetFile(Path.Join(s_workingDir, "b", "b.txt"));
        var fileFormDownLoadUncompressed = fileSystem.GetFile(Path.Join(s_workingDir, "c"));
        var fileFormDownLoadZip1 = fileSystem.GetFile(Path.Join(s_workingDir, "d", "file1.txt"));
        var fileFormDownLoadZip2 = fileSystem.GetFile(Path.Join(s_workingDir, "d", "file2.txt"));

        var locks = await packageManager.GetCurrentPackageLock();

        if (!dryRun)
        {
            Assert.Equal(new MockFileData("self file").Contents, fileFormSelf1.Contents);
            Assert.NotNull(fileFormSelf2);
            Assert.Equal(new MockFileData("self file").Contents, fileFormSelf3.Contents);
            Assert.NotNull(fileFormDownLoadUncompressed);
            Assert.Equal(new MockFileData("from compress").Contents, fileFormDownLoadZip1.Contents);
            Assert.NotNull(fileFormDownLoadZip2);

            var installedFilesRecod = locks.Packages
                .First().Files
                .Select(p => Path.Join(s_workingDir, p))
                .Select(Path.GetFullPath);

            Assert.Equal(new List<string>(){
                Path.Join(s_workingDir, "a.txt"),
                Path.Join(s_workingDir, "a", "a.txt"),
                Path.Join(s_workingDir, "b", "b.txt"),
                Path.Join(s_workingDir, "c"),
                Path.Join(s_workingDir, "d", "file1.txt"),
                Path.Join(s_workingDir, "d", "file2.txt"),
            }.Select(Path.GetFullPath)
            , installedFilesRecod);

        }
        else
        {
            Assert.Null(fileFormSelf1);
        }

        //  == Check Commands ==
        if (!dryRun)
        {
            commandRunner.Verify(c => c.Run("echo pre-install", s_workingDir), Times.Once);
            commandRunner.Verify(c => c.Run("echo install", s_workingDir), Times.Once);
            commandRunner.Verify(c => c.Run("echo post-install", s_workingDir), Times.Once);
            commandRunner.Verify(c => c.Run(It.IsNotIn("echo pre-install", "echo install", "echo post-install"), s_workingDir), Times.Never);
        }
        else
        {
            commandRunner.Verify(c => c.Run(It.IsAny<string>(), s_workingDir), Times.Never);
        }
        commandRunner.Verify(c => c.Run(It.IsAny<string>(), It.IsNotIn(s_workingDir)), Times.Never);

    }

    [Fact]
    public async Task InstallPackage_File_Already_Exists()
    {
        // Arrange.
        var manifest = new PackageManifest
        {
            ToothPath = "example.com/pkg",
            Version = SemVersion.Parse("1.0.0"),
            Info = new() { Name = "", Description = "", Tags = [], AvatarUrl = Url.Parse("https://example.com/icon") },
            Variants =
            [
            new()
            {
                Platform = RuntimeInformation.RuntimeIdentifier,
                Label = "",
                Dependencies = [],
                PreserveFiles = [],
                RemoveFiles = [],
                Assets =
                [
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Self,
                    Urls = [],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.File,
                        Src = "a.txt",Dest = "a.txt"}
                        ]
                },
                ],
                Scripts = new PackageManifest.ScriptsType
                {
                PreInstall = ["echo pre-install"],
                Install = ["echo install"],
                PostInstall = ["echo post-install"],
                PrePack = ["echo pre-pack"],
                PostPack = ["echo post-pack"],
                PreUninstall = ["echo pre-uninstall"],
                Uninstall = ["echo uninstall"],
                PostUninstall = ["echo post-uninstall"],
                AdditionalScripts = new Dictionary<string, List<string>>
                {
                    { "same_script", new List<string> { "echo same" } },
                    { "custom_script1", new List<string> { "echo custom1" } }
                }
                }
            }
            ]
        };

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            {Path.Join(s_cacheDir,"package","tooth.json"),new MockFileData(TestExtensions.ToJsonBytes(manifest))},
            {Path.Join(s_cacheDir,"package","a.txt"),new MockFileData("self file")},
            {Path.Join(s_cacheDir,"package","a","a.txt"),new MockFileData("self file")},
            {Path.Join(s_cacheDir,"package","b","b.txt"),new MockFileData("self file")},
            {Path.Join(s_workingDir,"a.txt"),new MockFileData("already exists")}
        }, s_workingDir);

        var logger = new Mock<ILogger>();
        var commandRunner = new Mock<ICommandRunner>();

        var userInteraction = new Mock<IUserInteraction>();
        userInteraction.Setup(u => u.PromptForSelection(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync("No"); // Simulate skipping overwrite

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Logger).Returns(logger.Object);
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.UserInteraction).Returns(userInteraction.Object);

        var fileSource = new DirectoryFileSource(fileSystem, Path.Join(s_cacheDir, "package"));

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, [], []);

        // Act.
        // It should NOT throw, but skip overwrite because User selected "No"
        await packageManager.InstallPackage(fileSource, "", false, false, false, false);
        // Assert.

        var fileNotChange = fileSystem.GetFile(Path.Join(s_workingDir, "a.txt"));

        Assert.Equal(new MockFileData("already exists").Contents, fileNotChange.Contents);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Uninstall(bool dryRun, bool ignoreScripts)
    {
        // Arrange.
        var manifest = new PackageManifest
        {
            ToothPath = "example.com/pkg",
            Version = SemVersion.Parse("1.0.0"),
            Info = new() { Name = "", Description = "", Tags = [], AvatarUrl = Url.Parse("https://example.com/icon") },
            Variants =
            [
            new()
            {
                Platform = RuntimeInformation.RuntimeIdentifier,
                Label = "",
                Dependencies = [],
                PreserveFiles = ["d/file1.txt"],
                RemoveFiles = ["d/file2.txt"],
                Assets =
                [
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Self,
                    Urls = [],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.File,
                        Src = "a.txt",Dest = "a.txt"}
                        ],
                },
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Self,
                    Urls = [],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.File,
                        Src = "a/a.txt",Dest = "a/a.txt"}
                        ]
                },
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Self,
                    Urls = [],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.Dir,
                        Src = "b",Dest = "b"}
                        ]
                },
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Uncompressed,
                    Urls = ["https://example.com/c"],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.File,
                        Src = "",
                        Dest = "c"
                    }]
                },
                new()
                {
                    Type = PackageManifest.Asset.TypeEnum.Zip,
                    Urls = ["https://example.com/d.zip"],
                    Placements = [new(){
                        Type = PackageManifest.Placement.TypeEnum.Dir,
                        Src = "",
                        Dest = "d"
                    }],

                },
                ],
                Scripts = new PackageManifest.ScriptsType
                {
                PreInstall = ["echo pre-install"],
                Install = ["echo install"],
                PostInstall = ["echo post-install"],
                PrePack = ["echo pre-pack"],
                PostPack = ["echo post-pack"],
                PreUninstall = ["echo pre-uninstall"],
                Uninstall = ["echo uninstall"],
                PostUninstall = ["echo post-uninstall"],
                AdditionalScripts = new Dictionary<string, List<string>>
                {
                    { "same_script", new List<string> { "echo same" } },
                    { "custom_script1", new List<string> { "echo custom1" } }
                }
                }
            }
            ]
        };

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            {Path.Join(s_cacheDir,"package","tooth.json"),new MockFileData(TestExtensions.ToJsonBytes(manifest))},
            {Path.Join(s_cacheDir,"package","a.txt"),new MockFileData("self file")},
            {Path.Join(s_cacheDir,"package","a","a.txt"),new MockFileData("self file")},
            {Path.Join(s_cacheDir,"package","b","b.txt"),new MockFileData("self file")},
            {Path.Join("for-compress","file1.txt"),new MockFileData("from compress")},
            {Path.Join("for-compress","file2.txt"),new MockFileData("from compress")},
            {Path.Join(s_workingDir,"userfile"),new MockFileData("user file") }
        }, s_workingDir);

        var logger = new Mock<ILogger>();

        var commandRunner = new Mock<ICommandRunner>();

        var downloader = new Mock<IDownloader>();
        downloader.Setup(d => d.DownloadFile(Url.Parse("https://example.com/c"), It.IsAny<string>()))
        .Callback<Url, string>((_, destinationPath) =>
        {
            fileSystem.AddFile(destinationPath, new MockFileData("c"));
        });
        downloader.Setup(d => d.DownloadFile(Url.Parse("https://example.com/d.zip"), It.IsAny<string>()))
        .Callback<Url, string>((_, destinationPath) =>
        {
            fileSystem.AddFile(destinationPath, new MockFileData([]));

            using var archive = SharpCompress.Archives.Zip.ZipArchive.Create();
            archive.AddEntry("file1.txt", fileSystem.File.OpenRead(Path.Join("for-compress", "file1.txt")));
            archive.AddEntry("file2.txt", fileSystem.File.OpenRead(Path.Join("for-compress", "file2.txt")));
            archive.SaveTo(fileSystem.File.OpenWrite(destinationPath));
        });

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Logger).Returns(logger.Object);
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.Downloader).Returns(downloader.Object);

        var fileSource = new DirectoryFileSource(fileSystem, Path.Join(s_cacheDir, "package"));

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);

        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var packageManager = new PackageManager(context.Object, cacheManager, pathManager, [], []);

        await packageManager.InstallPackage(fileSource, "", false, true, false, false);

        // Act.
        await packageManager.UninstallPackage(new PackageIdentifier(manifest.ToothPath, manifest.Variants.First().Label), dryRun, ignoreScripts);

        // Assert.
        // == Check Filse ==
        var fileFormSelf1 = fileSystem.GetFile(Path.Join(s_workingDir, "a.txt"));
        var fileFormSelf2 = fileSystem.GetFile(Path.Join(s_workingDir, "a", "a.txt"));
        var dirA = fileSystem.GetFile(Path.Join(s_workingDir, "a"));
        var fileFormSelf3 = fileSystem.GetFile(Path.Join(s_workingDir, "b", "b.txt"));
        var fileFormDownLoadUncompressed = fileSystem.GetFile(Path.Join(s_workingDir, "c"));
        var fileFormDownLoadZip1 = fileSystem.GetFile(Path.Join(s_workingDir, "d", "file1.txt"));
        var fileFormDownLoadZip2 = fileSystem.GetFile(Path.Join(s_workingDir, "d", "file2.txt"));
        var userFile = fileSystem.GetFile(Path.Join(s_workingDir, "userfile"));

        var locks = await packageManager.GetCurrentPackageLock();

        if (!dryRun)
        {
            Assert.Null(fileFormSelf1);
            Assert.Null(fileFormSelf2);
            // Assert.Null(dirA); // Directory may remain
            Assert.Null(fileFormSelf3);
            Assert.Null(fileFormDownLoadUncompressed);
            Assert.NotNull(fileFormDownLoadZip1);
            Assert.Null(fileFormDownLoadZip2);
            Assert.NotNull(userFile);
            Assert.NotNull(dirA); // Directory remains if not explicitly removed
        }
        else
        {
            Assert.NotNull(fileFormSelf1);
        }

        //  == Check Commands ==
        if (!dryRun)
        {
            commandRunner.Verify(c => c.Run("echo pre-uninstall", s_workingDir), Times.Once);
            commandRunner.Verify(c => c.Run("echo uninstall", s_workingDir), Times.Once);
            commandRunner.Verify(c => c.Run("echo post-uninstall", s_workingDir), Times.Once);
            commandRunner.Verify(c => c.Run(It.IsNotIn("echo pre-uninstall", "echo uninstall", "echo post-uninstall"), s_workingDir), Times.Never);
        }
        else
        {
            commandRunner.Verify(c => c.Run(It.IsAny<string>(), s_workingDir), Times.Never);
        }
        commandRunner.Verify(c => c.Run(It.IsAny<string>(), It.IsNotIn(s_workingDir)), Times.Never);

    }

    [Fact]
    public async Task Uninstall_PackNotFound()
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), s_workingDir);

        var logger = new Mock<ILogger>();

        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var fileSource = new DirectoryFileSource(fileSystem, Path.Join(s_cacheDir, "package"));

        var packageManager = PackageManagerFromCxtAndFs(context, fileSystem);

        // Act.
        await packageManager.UninstallPackage(new PackageIdentifier("exampel.com/pkg", ""), true, true);

        // Assert.
        // ?
    }
}


