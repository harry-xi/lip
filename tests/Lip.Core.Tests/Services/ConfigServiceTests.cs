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
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { Path.Combine(Path.GetDirectoryName(configPath)!, "placeholder.txt"), new MockFileData("") }
        });
        var mockUserInteraction = new Mock<IUserInteraction>();
        var service = new ConfigService(mockFileSystem, mockUserInteraction.Object);

        // Act
        var result = await service.LoadConfig();

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
        var config = new RuntimeConfig();
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { configPath, new MockFileData(json) }
        });
        var mockUserInteraction = new Mock<IUserInteraction>();
        var service = new ConfigService(mockFileSystem, mockUserInteraction.Object);

        // Act
        var result = await service.LoadConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.FormatVersion);
    }

    [Fact]
    public async Task SaveConfig_CreatesFileWithConfig()
    {
        // Arrange
        var configPath = GetConfigPath();
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { Path.Combine(Path.GetDirectoryName(configPath)!, "placeholder.txt"), new MockFileData("") }
        });
        var mockUserInteraction = new Mock<IUserInteraction>();
        var service = new ConfigService(mockFileSystem, mockUserInteraction.Object);
        var config = new RuntimeConfig();

        // Act
        await service.SaveConfig(config);

        // Assert
        Assert.True(mockFileSystem.File.Exists(configPath));
        var content = mockFileSystem.File.ReadAllText(configPath);
        Assert.Contains("format_version", content);
    }
}