using Lip.Core.Context;
using Lip.Core.Services;
using Moq;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests;

public class ConfigServiceTests
{
    private static readonly string s_runtimeConfigPath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");

    [Fact]
    public async Task ConfigDelete_SingleItem_ResetsToDefault()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new()
        {
            Cache = "/custom/cache",
            GitHubProxies = ["https://github-proxy.com"],
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { s_runtimeConfigPath, new MockFileData(initialRuntimeConfig.ToJsonBytes()) },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        await configService.Delete("github_proxies");

        // Assert.
        Assert.True(fileSystem.File.Exists(s_runtimeConfigPath));

        Assert.Equal($$"""
        {
            "cache": "/custom/cache",
            "github_proxies": "https://github.com,https://github.levimc.org",
            "go_module_proxies": "https://goproxy.io"
        }
        """.ReplaceLineEndings(), fileSystem.File.ReadAllText(s_runtimeConfigPath));
    }

    [Fact]
    public async Task ConfigDelete_RcFileNotExists_CreatesNewFile()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new()
        {
            Cache = "/custom/cache",
            GitHubProxies = ["https://github-proxy.com"],
            GoModuleProxies = ["https://custom-proxy.io"],
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>());

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        await configService.Delete("github_proxies");

        // Assert.
        Assert.True(fileSystem.File.Exists(s_runtimeConfigPath));

        Assert.Equal($$"""
        {
            "cache": "/custom/cache",
            "github_proxies": "https://github.com,https://github.levimc.org",
            "go_module_proxies": "https://custom-proxy.io"
        }
        """.ReplaceLineEndings(), fileSystem.File.ReadAllText(s_runtimeConfigPath));
    }

    [Fact]
    public async Task ConfigDelete_UnknownKey_ThrowsArgumentException()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new();

        Mock<IContext> context = new();

        MockFileSystem fileSystem = new();
        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act & Assert.
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => configService.Delete("unknown"));
        Assert.Equal("Unknown configuration key: 'unknown'. (Parameter 'key')", exception.Message);
    }

    [Fact]
    public void ConfigGet_SingleItem_Passes()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new() { Cache = "/custom/cache" };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { s_runtimeConfigPath, new MockFileData(initialRuntimeConfig.ToJsonBytes()) },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        string result = configService.Get("cache");

        // Assert.
        Assert.Equal("/custom/cache", result);
    }

    [Fact]
    public void ConfigGet_UnknownKey_ThrowsArgumentException()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new();

        Mock<IContext> context = new();

        MockFileSystem fileSystem = new();
        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act & Assert.
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configService.Get("unknown"));
        Assert.Equal("Unknown configuration key: 'unknown'. (Parameter 'key')", exception.Message);
    }

    [Fact]
    public void ConfigList_ReturnsAllConfigurations()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new()
        {
            Cache = "/custom/cache",
            GitHubProxies = ["https://github-proxy.com"],
            GoModuleProxies = ["https://custom-proxy.io"],
        };

        Mock<IContext> context = new();

        MockFileSystem fileSystem = new();
        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        Dictionary<string, string> result = configService.List();

        // Assert.
        Assert.Equal(3, result.Count);
        Assert.Equal("/custom/cache", result["cache"]);
        Assert.Equal("https://github-proxy.com", result["github_proxies"]);
        Assert.Equal("https://custom-proxy.io", result["go_module_proxies"]);
    }

    [Fact]
    public async Task ConfigSet_SingleItem_Passes()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new();

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { s_runtimeConfigPath, new MockFileData(initialRuntimeConfig.ToJsonBytes()) },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        await configService.Set("cache", "/path/to/cache");

        // Assert.
        Assert.True(fileSystem.File.Exists(s_runtimeConfigPath));

        Assert.Equal($$"""
        {
            "cache": "/path/to/cache",
            "github_proxies": "https://github.com,https://github.levimc.org",
            "go_module_proxies": "https://goproxy.io"
        }
        """.ReplaceLineEndings(), fileSystem.File.ReadAllText(s_runtimeConfigPath));
    }

    [Fact]
    public async Task ConfigSet_RcFileNotExists_CreatesNewFile()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new();

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>());

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        await configService.Set("cache", "/path/to/cache");

        // Assert.
        Assert.True(fileSystem.File.Exists(s_runtimeConfigPath));

        Assert.Equal($$"""
        {
            "cache": "/path/to/cache",
            "github_proxies": "https://github.com,https://github.levimc.org",
            "go_module_proxies": "https://goproxy.io"
        }
        """.ReplaceLineEndings(), fileSystem.File.ReadAllText(s_runtimeConfigPath));
    }

    [Fact]
    public async Task ConfigSet_UnknownItem_ThrowsArgumentException()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new();

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { s_runtimeConfigPath, new MockFileData(initialRuntimeConfig.ToJsonBytes()) },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        ArgumentException argumentException = await Assert.ThrowsAsync<ArgumentException>(
            () => configService.Set("unknown", "value"));

        // Assert.
        Assert.Equal("Unknown configuration key: 'unknown'. (Parameter 'key')", argumentException.Message);
    }
}