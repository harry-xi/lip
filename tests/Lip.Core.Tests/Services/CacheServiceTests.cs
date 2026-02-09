using Lip.Core.Services;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Lip.Core.Tests.Services;

public class CacheServiceTests
{
    private static string GetCachePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "lip", "cache");

    [Fact]
    public async Task GetOrCreateDirectory_NonExistent_CreatesAndCallsFactory()
    {
        // Arrange
        var cachePath = GetCachePath();
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), cachePath);
        var service = new CacheService(mockFileSystem);
        bool factoryCalled = false;

        // Act
        var result = await service.GetOrCreateDirectory("test-key", async dir =>
        {
            factoryCalled = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.True(factoryCalled);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOrCreateDirectory_Exists_DoesNotCallFactory()
    {
        // Arrange
        var cachePath = GetCachePath();
        var encodedKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-key"));
        var targetPath = Path.Combine(cachePath, encodedKey);

        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { Path.Combine(targetPath, "placeholder.txt"), new MockFileData("") }
        });
        var service = new CacheService(mockFileSystem);
        bool factoryCalled = false;

        // Act
        var result = await service.GetOrCreateDirectory("test-key", async dir =>
        {
            factoryCalled = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.False(factoryCalled);
    }

    [Fact]
    public async Task GetOrCreateFile_NonExistent_CreatesAndCallsFactory()
    {
        // Arrange
        var cachePath = GetCachePath();
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { Path.Combine(cachePath, "placeholder.txt"), new MockFileData("") }
        }, cachePath);
        var service = new CacheService(mockFileSystem);
        bool factoryCalled = false;

        // Act
        var result = await service.GetOrCreateFile("test-key", async file =>
        {
            factoryCalled = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.True(factoryCalled);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Clean_DeletesCacheDirectory()
    {
        // Arrange
        var cachePath = GetCachePath();
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { Path.Combine(cachePath, "somefile.txt"), new MockFileData("cached data") }
        });
        var service = new CacheService(mockFileSystem);

        // Act
        await service.Clean();

        // Assert - directory should be deleted
        Assert.False(mockFileSystem.Directory.Exists(cachePath));
    }
}