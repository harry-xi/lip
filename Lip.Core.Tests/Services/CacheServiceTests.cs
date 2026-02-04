using Lip.Core.Context;
using Lip.Core.Services;
using Moq;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.Services;

public class CacheServiceTests
{
    private static readonly string s_cacheDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "cache")
        : Path.Join("/", "path", "to", "cache");

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

        var cacheService = new CacheService(cacheManager);

        // Act.
        await cacheService.Clean();

        // Assert.
        Assert.False(fileSystem.File.Exists(Path.Join(s_cacheDir, "file")));
    }
}