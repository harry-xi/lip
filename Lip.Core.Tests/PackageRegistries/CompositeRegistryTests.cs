using Flurl;
using Flurl.Http.Testing;
using Lip.Core.Context;
using Lip.Core.PackageRegistries;
using Microsoft.Extensions.Logging;
using Moq;
using Semver;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.PackageRegistries;

public class CompositeRegistryTests
{
    private record ListRemoteResultItem(string Sha, string Ref) : IGit.IListRemoteResultItem;

    private static readonly string s_cacheDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "cache")
        : Path.Join("/", "path", "to", "cache");

    private static readonly string s_workingDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "working")
        : Path.Join("/", "path", "to", "working");

    [Fact]
    public async Task FallsBack_ToGit()
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
        var goProxyRegistry = new GoProxyRegistry(cacheManager, pathManager, Url.Parse("https://example.com"));
        // Git (Success)
        var gitRegistry = new GitRegistry(context.Object.Git!, Url.Parse("https://github.com"));

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
    public async Task AllFail_ThrowsAggregateException()
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
        var goProxyRegistry = new GoProxyRegistry(cacheManager, pathManager, Url.Parse("https://example.com"));
        // Git (Fail)
        var gitRegistry = new GitRegistry(context.Object.Git!, Url.Parse("https://github.com"));

        var compositeRegistry = new CompositeRegistry([goProxyRegistry, gitRegistry]);

        // Act & Assert.
        using var httpTest = new HttpTest();
        httpTest.RespondWith("Not Found", 404); // GoProxy fails

        await Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await compositeRegistry.GetVersions(packageIdentifier);
        });
    }
}