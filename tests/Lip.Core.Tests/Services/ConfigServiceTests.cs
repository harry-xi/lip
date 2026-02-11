using Flurl;
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
    public async Task List_FileNotExists_ReturnsDefaultAndSaves()
    {
        // Arrange
        string configPath = GetConfigPath();
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { Path.Combine(Path.GetDirectoryName(configPath)!, "placeholder.txt"), new MockFileData("") }
        });
        Mock<IUserInteraction> mockUserInteraction = new();
        ConfigService service = new(mockFileSystem, mockUserInteraction.Object);

        // Act
        var result = await service.List();

        // Assert
        Assert.NotNull(result);
        Assert.True(mockFileSystem.File.Exists(configPath));
    }

    [Fact]
    public async Task Get_ValidFile_ReturnsConfig()
    {
        // Arrange
        string configPath = GetConfigPath();
        RuntimeConfig config = new() { GithubProxy = new Url("https://proxy.com") };
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { configPath, new MockFileData(json) }
        });
        Mock<IUserInteraction> mockUserInteraction = new();
        ConfigService service = new(mockFileSystem, mockUserInteraction.Object);

        // Act
        string result = await service.Get("github_proxy");

        // Assert
        Assert.Equal("https://proxy.com", result);
    }

    [Fact]
    public async Task Set_CreatesFileWithConfig()
    {
        // Arrange
        string configPath = GetConfigPath();
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { Path.Combine(Path.GetDirectoryName(configPath)!, "placeholder.txt"), new MockFileData("") }
        });
        Mock<IUserInteraction> mockUserInteraction = new();
        ConfigService service = new(mockFileSystem, mockUserInteraction.Object);

        // Act
        await service.Set("github_proxy", "https://new.com");

        // Assert
        Assert.True(mockFileSystem.File.Exists(configPath));
        string content = mockFileSystem.File.ReadAllText(configPath);
        Assert.Contains("https://new.com", content);
    }
}