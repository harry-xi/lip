using Lip.Core.Entities;
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
    private readonly Mock<ILogger> _loggerMock;
    private readonly ConfigService _configService;
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigServiceTests()
    {
        _fileSystem = new MockFileSystem();
        _loggerMock = new Mock<ILogger>();
        _configService = new ConfigService(_fileSystem, _loggerMock.Object);

        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");

        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    [Fact]
    public async Task LoadConfig_FileDoesNotExist_ReturnsDefaultAndCreatesFile()
    {
        // Act
        var result = await _configService.LoadConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.FormatVersion); // Default version
        Assert.True(_fileSystem.File.Exists(_configPath));

        // Verify logs
        VerifyLog(LogLevel.Error, times: Times.Once()); // Exception caught
        VerifyLog(LogLevel.Information, "Using default runtime configuration", Times.Once());
    }

    [Fact]
    public async Task LoadConfig_FileExistsAndIsValid_ReturnsDeserializedConfig()
    {
        // Arrange
        var expectedConfig = new RuntimeConfig { FormatVersion = 3, FormatUuid = "289f771f-2c9a-4d73-9f3f-8492495a924d" };
        var json = JsonSerializer.Serialize(expectedConfig, _jsonOptions);

        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        await _fileSystem.File.WriteAllTextAsync(_configPath, json);

        // Act
        var result = await _configService.LoadConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConfig.FormatVersion, result.FormatVersion);
        Assert.Equal(expectedConfig.FormatUuid, result.FormatUuid);
    }

    [Fact]
    public async Task LoadConfig_FileExistsButIsInvalidJson_ReturnsDefaultAndOverwritesFile()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        await _fileSystem.File.WriteAllTextAsync(_configPath, "invalid json");

        // Act
        var result = await _configService.LoadConfig();

        // Assert
        Assert.NotNull(result);
        // Should return default
        Assert.Equal(3, result.FormatVersion);

        // Should overwrite file with valid default config
        var fileContent = await _fileSystem.File.ReadAllTextAsync(_configPath);
        var savedConfig = JsonSerializer.Deserialize<RuntimeConfig>(fileContent);
        Assert.NotNull(savedConfig);

        // Verify logs
        VerifyLog(LogLevel.Error, times: Times.Once());
        VerifyLog(LogLevel.Information, "Using default runtime configuration", Times.Once());
    }

    [Fact]
    public async Task SaveConfig_WritesConfigToFile()
    {
        // Arrange
        var config = new RuntimeConfig(); // Default

        // Act
        await _configService.SaveConfig(config);

        // Assert
        Assert.True(_fileSystem.File.Exists(_configPath));
        var fileContent = await _fileSystem.File.ReadAllTextAsync(_configPath);
        var savedConfig = JsonSerializer.Deserialize<RuntimeConfig>(fileContent);
        Assert.NotNull(savedConfig);
        Assert.Equal(config.FormatVersion, savedConfig.FormatVersion);
    }

    private void VerifyLog(LogLevel level, string? messageContains = null, Times? times = null)
    {
        _loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => messageContains == null || v.ToString()!.Contains(messageContains)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.AtLeastOnce());
    }
}