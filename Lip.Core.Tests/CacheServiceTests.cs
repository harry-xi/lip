using Flurl;
using Lip.Core.PackageRegistries;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Lip.Core.Tests;

public class CacheServiceTests
{
    private static readonly string s_cacheDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "cache")
        : Path.Join("/", "path", "to", "cache");



    [Fact]
    public async Task CacheAdd_ValidPackageSpecifier_AddsCache()
    {
        // Arrange.
        string packageManifestData = $$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/repo",
                "version": "1.0.0",
                "variants": [
                    {
                        "platform": "{{RuntimeInformation.RuntimeIdentifier}}",
                        "assets": [
                            {
                                "type": "self"
                            },
                            {
                                "type": "zip"
                            },
                            {
                                "type": "zip",
                                "urls": [
                                    "https://example.com/test.file"
                                ]
                            }
                        ]
                    }
                ]
            }
            """;

        RuntimeConfig runtimeConfig = new()
        {
            Cache = s_cacheDir,
            GoModuleProxies = []
        };

        MockFileSystem fileSystem = new();

        Mock<IDownloader> downloader = new();
        Mock<IGit> git = new();
        Mock<ILogger> logger = new();

        Mock<IContext> context = new();
        context.SetupGet(c => c.Downloader).Returns(downloader.Object);
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(git.Object);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var pathManager = new PathManager(fileSystem, s_cacheDir, s_cacheDir);
        var cacheManager = new CacheManager(context.Object, pathManager, [], []);

        var testFileUrl = Url.Parse("https://example.com/test.file");
        var testFilePath = pathManager.GetDownloadedFileCachePath(testFileUrl);
        var repoUrl = Url.Parse("https://example.com/repo");
        var repoDirPath = pathManager.GetGitRepoDirCachePath(repoUrl, "v1.0.0");

        downloader.Setup(d => d.DownloadFile(testFileUrl, testFilePath))
            .Callback<Url, string>((_, dest) =>
            {
                fileSystem.AddFile(dest, new MockFileData("test"));
            });

        git.Setup(g => g.Clone(repoUrl.ToString(), repoDirPath, "v1.0.0", 1))
            .Callback<string, string, string?, int?>((_, dest, __, ___) =>
            {
                fileSystem.AddFile(Path.Join(dest, "tooth.json"), new MockFileData(packageManifestData));
            });

        var packageRegistryMock = new Mock<IPackageRegistry>();
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(PackageManifest.FromJsonElement(JsonDocument.Parse(packageManifestData).RootElement));

        var cacheService = new Services.CacheService(packageRegistryMock.Object, cacheManager);

        // Act.
        await cacheService.Add("example.com/repo@1.0.0");

        // Assert.
        Assert.True(fileSystem.File.Exists(testFilePath));
        Assert.True(fileSystem.File.Exists(Path.Join(repoDirPath, "tooth.json")));
    }

    [Fact]
    public async Task CacheAdd_NullVariant_AddsCache()
    {
        // Arrange.
        string packageManifestData = $$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/repo",
                "version": "1.0.0"
            }
            """;

        RuntimeConfig runtimeConfig = new()
        {
            Cache = s_cacheDir,
            GoModuleProxies = []
        };

        MockFileSystem fileSystem = new();

        Mock<IGit> git = new();
        git.Setup(g => g.Clone(
            "https://example.com/repo",
            Path.Join(s_cacheDir, "git_repos", "https%3A%2F%2Fexample.com%2Frepo", "v1.0.0"),
            "v1.0.0",
            1))
            .Callback<string, string, string?, int?>((_, dest, __, ___) =>
            {
                fileSystem.AddFile(Path.Join(dest, "tooth.json"), new MockFileData(packageManifestData));
            });

        var logger = new Mock<ILogger>();

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(git.Object);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var pathManager = new PathManager(fileSystem, s_cacheDir, s_cacheDir);
        var cacheManager = new CacheManager(context.Object, pathManager, [], []);
        var packageRegistryMock = new Mock<IPackageRegistry>();
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(PackageManifest.FromJsonElement(JsonDocument.Parse(packageManifestData).RootElement));

        var cacheService = new Services.CacheService(packageRegistryMock.Object, cacheManager);

        // Act.
        await cacheService.Add("example.com/repo@1.0.0");

        // Assert.
        Assert.True(fileSystem.File.Exists(Path.Join(s_cacheDir, "git_repos", "https%3A%2F%2Fexample.com%2Frepo", "v1.0.0", "tooth.json")));
    }

    [Fact]
    public async Task CacheAdd_MismatchedToothPath_ThrowsInvalidOperationException()
    {
        // Arrange.
        string packageManifestData = $$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/other-repo",
                "version": "1.0.0"
            }
            """;

        RuntimeConfig runtimeConfig = new()
        {
            Cache = s_cacheDir,
            GoModuleProxies = []
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
        {
                Path.Join(s_cacheDir, "git_repos", "https%3A%2F%2Fexample.com%2Frepo", "v1.0.0", "tooth.json"),
                new MockFileData(packageManifestData)
        },
        });

        Mock<ILogger> logger = new();

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(new Mock<IGit>().Object);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var pathManager = new PathManager(fileSystem, s_cacheDir, s_cacheDir);
        var cacheManager = new CacheManager(context.Object, pathManager, [], []);
        var packageRegistryMock = new Mock<IPackageRegistry>();
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(PackageManifest.FromJsonElement(JsonDocument.Parse(packageManifestData).RootElement));

        var cacheService = new Services.CacheService(packageRegistryMock.Object, cacheManager);

        // Act & Assert.
        await Assert.ThrowsAsync<InvalidOperationException>(() => cacheService.Add("example.com/repo@1.0.0"));
    }

    [Fact]
    public async Task CacheAdd_MismatchedVersion_ThrowsInvalidOperationException()
    {
        // Arrange.
        string packageManifestData = $$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/repo",
                "version": "2.0.0"
            }
            """;

        RuntimeConfig runtimeConfig = new()
        {
            Cache = s_cacheDir,
            GoModuleProxies = []
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
        {
                Path.Join(s_cacheDir, "git_repos", "https%3A%2F%2Fexample.com%2Frepo", "v1.0.0", "tooth.json"),
                new MockFileData(packageManifestData)
        },
        });

        Mock<ILogger> logger = new();

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Git).Returns(new Mock<IGit>().Object);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var pathManager = new PathManager(fileSystem, s_cacheDir, s_cacheDir);
        var cacheManager = new CacheManager(context.Object, pathManager, [], []);
        var packageRegistryMock = new Mock<IPackageRegistry>();
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(PackageManifest.FromJsonElement(JsonDocument.Parse(packageManifestData).RootElement));

        var cacheService = new Services.CacheService(packageRegistryMock.Object, cacheManager);

        // Act & Assert.
        await Assert.ThrowsAsync<InvalidOperationException>(() => cacheService.Add("example.com/repo@1.0.0"));
    }

    [Fact]
    public async Task CacheClean_WhenCalled_CleansCache()
    {
        // Arrange.
        RuntimeConfig runtimeConfig = new()
        {
            Cache = s_cacheDir,
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { Path.Join(s_cacheDir, "file"), new MockFileData("content") },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_cacheDir, s_cacheDir);
        var cacheManager = new CacheManager(context.Object, pathManager, [], []);
        var packageRegistryMock = new Mock<IPackageRegistry>();

        var cacheService = new Services.CacheService(packageRegistryMock.Object, cacheManager);

        // Act.
        await cacheService.Clean();

        // Assert.
        Assert.False(fileSystem.File.Exists(Path.Join(s_cacheDir, "file")));
    }

    [Fact]
    public async Task CacheList_WhenCalled_ReturnsCacheInfo()
    {
        // Arrange.
        RuntimeConfig runtimeConfig = new()
        {
            Cache = s_cacheDir,
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { Path.Join(s_cacheDir, "downloaded_files", "https%3A%2F%2Fexample.com%2Ftest.file"), new MockFileData("test") },
            { Path.Join(s_cacheDir, "git_repos", "https%3A%2F%2Fexample.com%2Frepo", "v1.0.0"), new MockDirectoryData() },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_cacheDir, s_cacheDir);
        var cacheManager = new CacheManager(context.Object, pathManager, [], []);
        var packageRegistryMock = new Mock<IPackageRegistry>();

        var cacheService = new Services.CacheService(packageRegistryMock.Object, cacheManager);

        var (downloadedFiles, gitRepos) = await cacheService.List();

        // Assert.
        Assert.Equal(new[] { "https://example.com/test.file" }, downloadedFiles);
        Assert.Equal(new[] { "https://example.com/repo v1.0.0" }, gitRepos);
    }
}