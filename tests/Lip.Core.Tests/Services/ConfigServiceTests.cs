using Lip.Core.Entities;
using Lip.Core.Json;
using Lip.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Xunit;

namespace Lip.Core.Tests.Services;

public class ConfigServiceTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly Mock<ILogger<ConfigService>> _loggerMock;
    private readonly ConfigService _configService;
    private readonly string _configPath;

    public ConfigServiceTests()
    {
        _fileSystem = new MockFileSystem();
        _loggerMock = new Mock<ILogger<ConfigService>>();
        _configService = new ConfigService(_fileSystem, _loggerMock.Object);
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");
    }

    [Fact]
    public async Task List_ReturnsAllNonNullProperties()
    {
        // Arrange
        var config = new RuntimeConfig
        {
            GithubProxy = "https://proxy.example.com",
            GoModuleProxy = "https://goproxy.example.com"
        };
        await SaveConfigAsync(config);

        // Act
        var result = await _configService.List();

        // Assert
        Assert.Contains("format_version", result.Keys);
        Assert.Contains("format_uuid", result.Keys);
        Assert.Equal("https://proxy.example.com", result["github_proxy"]);
        Assert.Equal("https://goproxy.example.com", result["go_module_proxy"]);
    }

    [Fact]
    public async Task Get_ExistingKey_ReturnsCorrectValue()
    {
        // Arrange
        var config = new RuntimeConfig
        {
            GithubProxy = "https://proxy.example.com"
        };
        await SaveConfigAsync(config);

        // Act
        var result = await _configService.Get("github_proxy");

        // Assert
        Assert.Equal("https://proxy.example.com", result);
    }

    [Fact]
    public async Task Get_NonExistingKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        await SaveConfigAsync(new RuntimeConfig());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _configService.Get("non_existent_key"));
    }

    [Fact]
    public async Task Set_ValidKey_UpdatesValue()
    {
        // Arrange
        await SaveConfigAsync(new RuntimeConfig());

        // Act
        await _configService.Set("github_proxy", "https://newproxy.example.com");

        // Assert
        var result = await _configService.Get("github_proxy");
        Assert.Equal("https://newproxy.example.com", result);
    }

    [Fact]
    public async Task Set_NonExistingKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        await SaveConfigAsync(new RuntimeConfig());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _configService.Set("non_existent_key", "value"));
    }

    [Fact]
    public async Task Set_ReadOnlyKey_ThrowsInvalidOperationException()
    {
        // Arrange
        await SaveConfigAsync(new RuntimeConfig());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _configService.Set("format_version", "100"));
    }

    [Fact]
    public async Task Delete_ExistingKey_SetsValueToNull()
    {
        // Arrange
        var config = new RuntimeConfig
        {
            GithubProxy = "https://proxy.example.com"
        };
        await SaveConfigAsync(config);

        // Act
        await _configService.Delete("github_proxy");

        // Assert
        var result = await _configService.Get("github_proxy");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task Delete_NonExistingKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        await SaveConfigAsync(new RuntimeConfig());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _configService.Delete("non_existent_key"));
    }

    [Fact]
    public async Task Delete_ReadOnlyKey_ThrowsInvalidOperationException()
    {
        // Arrange
        await SaveConfigAsync(new RuntimeConfig());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _configService.Delete("format_version"));
    }

    private async Task SaveConfigAsync(RuntimeConfig config)
    {
        if (!_fileSystem.Directory.Exists(Path.GetDirectoryName(_configPath)))
        {
            _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        }

        JsonSerializerOptions options = new()
        {
            Converters = { new UrlJsonConverter() }
        };

        using var stream = _fileSystem.File.Create(_configPath);
        await JsonSerializer.SerializeAsync(stream, config, options);
    }
}