using Flurl;
using Flurl.Http.Testing;
using Lip.Core.Context;
using Lip.Core.PackageRegistries;
using Microsoft.Extensions.Logging;
using Moq;
using Semver;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.PackageRegistries;

using static Lip.Core.PackageLock;

public class GoProxyRegistryTests
{
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

        var packageRegistry = new GoProxyRegistry(cacheManager, pathManager, Url.Parse("https://example.com"));

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
        var packageRegistry = new GoProxyRegistry(cacheManager, pathManager, Url.Parse("https://example.com"));

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
    public async Task GetVersions_Success()
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
        var packageRegistry = new GoProxyRegistry(cacheManager, pathManager, Url.Parse("https://example.com"));

        // Act.
        using var httpTest = new HttpTest();
        httpTest.RespondWith(versionFile);

        var result = await packageRegistry.GetVersions(new PackageIdentifier("example.com/user/repo", ""));

        // Assert.
        Assert.Equal(expectedVersions, result);
    }
}