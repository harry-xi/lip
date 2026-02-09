using Flurl;
using Lip.Core.Entities;
using Semver;
using Xunit;

namespace Lip.Core.Tests.Entities;

public class RuntimeConfigTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsCorrectDefaults()
    {
        var config = new RuntimeConfig();

        Assert.Equal(3, config.FormatVersion);
        Assert.Equal("289f771f-2c9a-4d73-9f3f-8492495a924d", config.FormatUuid);
        Assert.Null(config.GithubProxy);
        Assert.Equal("https://goproxy.io", config.GoModuleProxy.ToString());
    }

    [Fact]
    public void FormatVersion_InvalidVersion_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RuntimeConfig { FormatVersion = 999 });
    }

    [Fact]
    public void FormatUuid_InvalidUuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RuntimeConfig { FormatUuid = "invalid-uuid" });
    }

    [Fact]
    public void AsDictionary_ReturnsAllProperties()
    {
        var config = new RuntimeConfig
        {
            GithubProxy = new Url("https://proxy.example.com"),
            GoModuleProxy = new Url("https://custom.proxy.io")
        };

        var dict = config.AsDictionary();

        Assert.True(dict.ContainsKey("format_version"));
        Assert.True(dict.ContainsKey("format_uuid"));
        Assert.True(dict.ContainsKey("github_proxy"));
        Assert.True(dict.ContainsKey("go_module_proxy"));
    }

    [Fact]
    public void With_ValidKey_ReturnsNewConfigWithUpdatedValue()
    {
        var config = new RuntimeConfig();
        var newProxy = new Url("https://new.proxy.io");

        var newConfig = config.With("go_module_proxy", newProxy);

        Assert.NotSame(config, newConfig);
        Assert.Equal(newProxy, newConfig.GoModuleProxy);
    }

    [Fact]
    public void With_InvalidKey_ThrowsKeyNotFoundException()
    {
        var config = new RuntimeConfig();

        Assert.Throws<KeyNotFoundException>(() => config.With("invalid_key", "value"));
    }
}

public class WorkspaceStateTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsCorrectDefaults()
    {
        var state = new WorkspaceState();

        Assert.Equal(3, state.FormatVersion);
        Assert.Equal("289f771f-2c9a-4d73-9f3f-8492495a924d", state.FormatUuid);
        Assert.Empty(state.Packages);
    }

    [Fact]
    public void FormatVersion_InvalidVersion_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceState { FormatVersion = 999 });
    }

    [Fact]
    public void FormatUuid_InvalidUuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceState { FormatUuid = "invalid-uuid" });
    }
}

public class WorkspaceStatePackageTests
{
    [Fact]
    public void GetPackageSpec_ReturnsCorrectSpec()
    {
        var manifest = new PackageManifest
        {
            Path = "github.com/test/pkg",
            Version = new SemVersion(1, 0, 0)
        };
        var pkg = new WorkspaceStatePackage
        {
            Files = [],
            IsExplicit = true,
            Manifest = manifest,
            Variant = "win_x64"
        };

        var spec = pkg.GetPackageSpec();

        Assert.Equal("github.com/test/pkg", spec.Id.Path);
        Assert.Equal("win_x64", spec.Id.Variant);
        Assert.Equal(new SemVersion(1, 0, 0), spec.Version);
    }

    [Fact]
    public void Variant_Invalid_ThrowsArgumentException()
    {
        var manifest = new PackageManifest
        {
            Path = "github.com/test/pkg",
            Version = new SemVersion(1, 0, 0)
        };

        Assert.Throws<ArgumentException>(() => new WorkspaceStatePackage
        {
            Files = [],
            IsExplicit = true,
            Manifest = manifest,
            Variant = "INVALID-VARIANT!"
        });
    }
}