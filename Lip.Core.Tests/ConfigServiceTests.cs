using Lip.Core.Context;
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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        await configService.Delete(["github_proxies"]);

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
    public async Task ConfigDelete_MultipleItems_ResetsToDefault()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new()
        {
            Cache = "/custom/cache",
            GitHubProxies = ["https://github-proxy.com"],
            GoModuleProxies = ["https://custom-proxy.io"],
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { s_runtimeConfigPath, new MockFileData(initialRuntimeConfig.ToJsonBytes()) },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        await configService.Delete(["github_proxies", "go_module_proxies"]);

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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        await configService.Delete(["github_proxies"]);

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
    public async Task ConfigDelete_EmptyKeys_ThrowsArgumentException()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new();

        Mock<IContext> context = new();

        MockFileSystem fileSystem = new();
        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act & Assert.
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => configService.Delete([]));
        Assert.Equal("No configuration keys provided. (Parameter 'keys')", exception.Message);
    }

    [Fact]
    public async Task ConfigDelete_UnknownKey_ThrowsArgumentException()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new();

        Mock<IContext> context = new();

        MockFileSystem fileSystem = new();
        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act & Assert.
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => configService.Delete(["unknown"]));
        Assert.Equal("Unknown configuration key: 'unknown'. (Parameter 'keys')", exception.Message);
    }

    [Fact]
    public async Task ConfigDelete_PartialUnknownItem_ThrowsArgumentException()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new();

        Mock<IContext> context = new();

        MockFileSystem fileSystem = new();
        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act & Assert.
        ArgumentException argumentException = await Assert.ThrowsAsync<ArgumentException>(
            () => configService.Delete(["cache", "unknown"]));
        Assert.Equal("Unknown configuration key: 'unknown'. (Parameter 'keys')", argumentException.Message);
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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        Dictionary<string, string> result = configService.Get(["cache"]);

        // Assert.
        Assert.Single(result);
        Assert.Equal("/custom/cache", result["cache"]);
    }

    [Fact]
    public void ConfigGet_MultipleItems_Passes()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new()
        {
            Cache = "/custom/cache",
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { s_runtimeConfigPath, new MockFileData(initialRuntimeConfig.ToJsonBytes()) },
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        Dictionary<string, string> result = configService.Get(
            ["cache", "github_proxies"]);

        // Assert.
        Assert.Equal(2, result.Count);
        Assert.Equal("/custom/cache", result["cache"]);
        Assert.Equal("https://github.com,https://github.levimc.org", result["github_proxies"]);
    }

    [Fact]
    public void ConfigGet_UnknownKey_ThrowsArgumentException()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new();

        Mock<IContext> context = new();

        MockFileSystem fileSystem = new();
        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act & Assert.
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configService.Get(["unknown"]));
        Assert.Equal("Unknown configuration key: 'unknown'. (Parameter 'keys')", exception.Message);
    }

    [Fact]
    public void ConfigGet_PartialUnknownItem_ThrowsArgumentException()
    {
        // Arrange.
        RuntimeConfig initialRuntimeConfig = new();

        Mock<IContext> context = new();

        MockFileSystem fileSystem = new();
        var pathManager = new PathManager(fileSystem, "/", "/");
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act & Assert.
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configService.Get(["cache", "unknown"]));
        Assert.Equal("Unknown configuration key: 'unknown'. (Parameter 'keys')", exception.Message);
    }

    [Fact]
    public void ConfigGet_EmptyKeys_ReturnsAllConfigurations()
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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        // Act.
        Dictionary<string, string> result = configService.Get([]);

        // Assert.
        Assert.Equal(3, result.Count);
        Assert.Equal("/custom/cache", result["cache"]);
        Assert.Equal("https://github-proxy.com", result["github_proxies"]);
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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        Dictionary<string, string> keyValuePairs = new()
        {
            { "cache", "/path/to/cache" },
        };

        // Act.
        await configService.Set(keyValuePairs);

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
    public async Task ConfigSet_MultipleItems_Passes()
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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        Dictionary<string, string> keyValuePairs = new()
        {
            { "cache", "/path/to/cache" },
            { "github_proxies", "https://github.com" },
            { "go_module_proxies", "https://goproxy.io" },
        };

        // Act.
        await configService.Set(keyValuePairs);

        // Assert.
        Assert.True(fileSystem.File.Exists(s_runtimeConfigPath));

        Assert.Equal($$"""
        {
            "cache": "/path/to/cache",
            "github_proxies": "https://github.com",
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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        Dictionary<string, string> keyValuePairs = new()
        {
            { "cache", "/path/to/cache" },
        };

        // Act.
        await configService.Set(keyValuePairs);

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
    public async Task ConfigSet_NoItems_ThrowsArgumentException()
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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        Dictionary<string, string> keyValuePairs = [];

        // Act.
        ArgumentException argumentException = await Assert.ThrowsAsync<ArgumentException>(() => configService.Set(keyValuePairs));

        // Assert.
        Assert.Equal("No configuration items provided. (Parameter 'keyValuePairs')", argumentException.Message);
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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        Dictionary<string, string> keyValuePairs = new()
        {
            { "unknown", "value" },
        };



        // Act.
        ArgumentException argumentException = await Assert.ThrowsAsync<ArgumentException>(() => configService.Set(keyValuePairs));

        // Assert.
        Assert.Equal("Unknown configuration key: 'unknown'. (Parameter 'keyValuePairs')", argumentException.Message);
    }

    [Fact]
    public async Task ConfigSet_PartialUnknownItem_ThrowsArgumentException()
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
        var configService = new Services.ConfigService(initialRuntimeConfig, context.Object, pathManager);

        Dictionary<string, string> keyValuePairs = new()
        {
            { "cache", "/path/to/cache" },
            { "unknown", "value" },
        };

        // Act & Assert.
        ArgumentException argumentException = await Assert.ThrowsAsync<ArgumentException>(
            () => configService.Set(keyValuePairs));
        Assert.Equal("Unknown configuration key: 'unknown'. (Parameter 'keyValuePairs')", argumentException.Message);
    }
}