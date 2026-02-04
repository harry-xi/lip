using Flurl;
using Lip.Core.Context;
using Lip.Core.PackageRegistries;
using Moq;
using Semver;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.PackageRegistries;

public class GitRegistryTests
{
    private record ListRemoteResultItem(string Sha, string Ref) : IGit.IListRemoteResultItem;

    private static readonly string s_cacheDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "cache")
        : Path.Join("/", "path", "to", "cache");

    private static readonly string s_workingDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "working")
        : Path.Join("/", "path", "to", "working");

    [Fact]
    public async Task GetVersions_Success()
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
        var packageRegistry = new GitRegistry(context.Object.Git!, Url.Parse("https://github.com"));

        // Act.
        var result = await packageRegistry.GetVersions(new PackageIdentifier("example.com/user/repo", ""));

        // Assert.
        Assert.Equal(expectedVersions, result);
    }
}