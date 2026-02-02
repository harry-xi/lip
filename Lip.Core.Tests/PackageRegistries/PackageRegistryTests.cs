using Flurl;
using Flurl.Http.Testing;
using Lip.Core.Context;
using Lip.Core.PackageRegistries;
using Microsoft.Extensions.Logging;
using Moq;
using Semver;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests;

using static Lip.Core.PackageLock;

public class PackageRegistryTests
{
    private record ListRemoteResultItem(string Sha, string Ref) : IGit.IListRemoteResultItem;

    private static readonly string s_cacheDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "cache")
        : Path.Join("/", "path", "to", "cache");

    private static readonly string s_workingDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "working")
        : Path.Join("/", "path", "to", "working");

    private static PackageManifest CreateManifest(string toothPath = "example.com/pkg", string version = "1.0.0", List<PackageManifest.Variant>? variants = null)
    {
        return new PackageManifest
        {
            FormatVersion = DefaultFormatVersion,
            FormatUuid = DefaultFormatUuid,
            ToothPath = toothPath,
            Version = SemVersion.Parse(version),
            Info = new() { Name = "", Description = "", Tags = [], AvatarUrl = Url.Parse("https://example.com/icon") },
            Variants = variants ?? []
        };
    }

    private static readonly PackageManifest s_examplePackage_1 = CreateManifest(toothPath: "example.com/pkg", version: "1.0.0");

    [Fact]
    public async Task GetManifest_Found()
    {
        // Arrange.
        var expectedPackage = s_examplePackage_1;

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>{
            {
                Path.Join(s_cacheDir, "git_repos", "https%3A%2F%2Fexample.com%2Fpkg", "v1.0.0", "tooth.json"),
                new MockFileData(LipTestExtensions.ToJsonBytes(expectedPackage))
            },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(new Mock<IGit>().Object);
        context.SetupGet(c => c.Logger).Returns(new Mock<ILogger>().Object);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);
        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        // We can test any registry here since GetManifest is identical.
        var packageRegistry = new GoProxyRegistry(context.Object, cacheManager, pathManager, []);

        // Act.
        var pkg = await packageRegistry.GetManifest(new PackageSpecifier(
            new PackageIdentifier(expectedPackage.ToothPath, ""),
            expectedPackage.Version));

        // Assert.
        Assert.Equal(LipTestExtensions.ToJsonBytes(expectedPackage), LipTestExtensions.ToJsonBytes(pkg!));
    }

    [Fact]
    public async Task GetManifest_NotFound()
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(new Mock<IGit>().Object);
        context.SetupGet(c => c.Logger).Returns(new Mock<ILogger>().Object);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);
        var cacheManager = new CacheManager(context.Object, pathManager, [], []);
        var packageRegistry = new GoProxyRegistry(context.Object, cacheManager, pathManager, []);

        // Act.
        // Assert.
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await packageRegistry.GetManifest(new PackageSpecifier(
                new PackageIdentifier(s_examplePackage_1.ToothPath, ""),
                s_examplePackage_1.Version));
        });
    }

    [Fact]
    public async Task GoProxyRegistry_GetVersions_Success()
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
        var packageRegistry = new GoProxyRegistry(context.Object, cacheManager, pathManager, [Url.Parse("https://example.com")]);

        // Act.
        using var httpTest = new HttpTest();
        httpTest.RespondWith(versionFile);

        var result = await packageRegistry.GetVersions(new PackageIdentifier("example.com/user/repo", ""));

        // Assert.
        Assert.Equal(expectedVersions, result);
    }

    [Fact]
    public async Task GitRegistry_GetVersions_Success()
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
        var packageRegistry = new GitRegistry(context.Object, []);

        // Act.
        var result = await packageRegistry.GetVersions(new PackageIdentifier("example.com/user/repo", ""));

        // Assert.
        Assert.Equal(expectedVersions, result);
    }

    [Fact]
    public async Task CompositeRegistry_FallsBack_ToGit()
    {
        // Arrange.
        var packageIdentifier = new PackageIdentifier("example.com/user/repo", "");

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), s_workingDir);

        // Git Mock (Success)
        var git = new Mock<IGit>();
        git.Setup(g => g.ListRemote("https://example.com/user/repo", true, true))
        .Returns(
            Task<List<IGit.IListRemoteResultItem>>.Factory.StartNew(() => [
                new ListRemoteResultItem("123", "refs/tags/v0.1.0")
            ])
        );

        // Context Mock
        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(git.Object);
        context.SetupGet(c => c.Logger).Returns(new Mock<ILogger>().Object);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);
        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        // GoProxy (Fail)
        var goProxyRegistry = new GoProxyRegistry(context.Object, cacheManager, pathManager, [Url.Parse("https://example.com")]);
        // Git (Success)
        var gitRegistry = new GitRegistry(context.Object, []);

        var compositeRegistry = new CompositeRegistry([goProxyRegistry, gitRegistry]);

        // Act.
        using var httpTest = new HttpTest();
        httpTest.RespondWith("Not Found", 404); // GoProxy fails

        var result = await compositeRegistry.GetVersions(packageIdentifier);

        // Assert.
        Assert.Single(result);
        Assert.Equal(new SemVersion(0, 1, 0), result[0]);
    }

    [Fact]
    public async Task CompositeRegistry_AllFail_ThrowsAggregateException()
    {
        // Arrange.
        var packageIdentifier = new PackageIdentifier("example.com/user/repo", "");
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), s_workingDir);

        // Git Mock (Fail)
        var git = new Mock<IGit>();
        git.Setup(g => g.ListRemote(It.IsAny<string>(), true, true))
            .ThrowsAsync(new Exception("Git Failed"));

        // Context Mock
        var context = new Mock<IContext>();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(git.Object);
        context.SetupGet(c => c.Logger).Returns(new Mock<ILogger>().Object);

        var pathManager = new PathManager(fileSystem, baseCacheDir: s_cacheDir, workingDir: s_workingDir);
        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        // GoProxy (Fail)
        var goProxyRegistry = new GoProxyRegistry(context.Object, cacheManager, pathManager, [Url.Parse("https://example.com")]);
        // Git (Fail)
        var gitRegistry = new GitRegistry(context.Object, []);

        var compositeRegistry = new CompositeRegistry([goProxyRegistry, gitRegistry]);

        // Act & Assert.
        using var httpTest = new HttpTest();
        httpTest.RespondWith("Not Found", 404); // GoProxy fails

        await Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await compositeRegistry.GetVersions(packageIdentifier);
        });
    }
    [Fact]
    public async Task LiprRegistry_GetManifest_Success()
    {
        // Arrange.
        var expectedPackage = s_examplePackage_1;
        var packageSpecifier = new PackageSpecifier(
            new PackageIdentifier(expectedPackage.ToothPath, ""),
            expectedPackage.Version);

        // http://lipr.levimc.org/example.com/pkg/v1.0.0/tooth.json
        var expectedUrl = $"https://lipr.levimc.org/{expectedPackage.ToothPath}/v{expectedPackage.Version}/tooth.json";

        var liprRegistry = new LiprRegistry();

        // Act.
        using var httpTest = new HttpTest();
        httpTest.ForCallsTo(expectedUrl)
            .RespondWithJson(expectedPackage);

        var result = await liprRegistry.GetManifest(packageSpecifier);

        // Assert.
        Assert.Equal(LipTestExtensions.ToJsonBytes(expectedPackage), LipTestExtensions.ToJsonBytes(result));
    }

    [Fact]
    public async Task LiprRegistry_GetVersions_ThrowsNotImplemented()
    {
        // Arrange.
        var liprRegistry = new LiprRegistry();

        // Act & Assert.
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await liprRegistry.GetVersions(new PackageIdentifier("example.com/pkg", ""));
        });
    }
}