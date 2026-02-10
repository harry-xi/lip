using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.Services;
using Moq;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;

namespace Lip.Core.Tests.Services;

public class ConfigServiceTests
{
    private static string GetConfigPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");

    [Fact]
    public async Task LoadConfig_FileNotExists_ReturnsDefaultAndSaves()
    {
        // Arrange
        var configPath = GetConfigPath();
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { Path.Combine(Path.GetDirectoryName(configPath)!, "placeholder.txt"), new MockFileData("") }
        });
        Mock<IUserInteraction> mockUserInteraction = new();
        ConfigService service = new(mockFileSystem, mockUserInteraction.Object);

        // Act
        RuntimeConfig result = await service.LoadConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.FormatVersion);
        Assert.True(mockFileSystem.File.Exists(configPath));
    }

    [Fact]
    public async Task LoadConfig_ValidFile_ReturnsConfig()
    {
        // Arrange
        var configPath = GetConfigPath();
        RuntimeConfig config = new();
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { configPath, new MockFileData(json) }
        });
        Mock<IUserInteraction> mockUserInteraction = new();
        ConfigService service = new(mockFileSystem, mockUserInteraction.Object);

        // Act
        RuntimeConfig result = await service.LoadConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.FormatVersion);
    }

    [Fact]
    public async Task SaveConfig_CreatesFileWithConfig()
    {
        // Arrange
        var configPath = GetConfigPath();
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { Path.Combine(Path.GetDirectoryName(configPath)!, "placeholder.txt"), new MockFileData("") }
        });
        Mock<IUserInteraction> mockUserInteraction = new();
        ConfigService service = new(mockFileSystem, mockUserInteraction.Object);
        RuntimeConfig config = new();

        // Act
        await service.SaveConfig(config);

        // Assert
        Assert.True(mockFileSystem.File.Exists(configPath));
        var content = mockFileSystem.File.ReadAllText(configPath);
        Assert.Contains("format_version", content);
    }
}