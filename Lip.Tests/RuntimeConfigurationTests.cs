using System.Text;

namespace Lip.Tests;

public class RuntimeConfigurationTests
{
    [Fact]
    public void FromBytes_MinimumJson_Passes()
    {
        var runtimeConfiguration = RuntimeConfiguration.FromBytes("{}"u8.ToArray());

        Assert.Equal(
            OperatingSystem.IsWindows()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "lip-cache")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "lip"),
            runtimeConfiguration.Cache
        );
        Assert.True(runtimeConfiguration.Color);
        Assert.Equal("git", runtimeConfiguration.Git);
        Assert.Equal("", runtimeConfiguration.GitHubProxy);
        Assert.Equal("https://goproxy.io", runtimeConfiguration.GoModuleProxy);
        Assert.Equal("", runtimeConfiguration.HttpsProxy);
        Assert.Equal("", runtimeConfiguration.NoProxy);
        Assert.Equal("", runtimeConfiguration.Proxy);
        Assert.Equal(
            OperatingSystem.IsWindows()
                ? "cmd.exe"
                : "/bin/sh", runtimeConfiguration.ScriptShell
        );
    }

    [Fact]
    public void FromBytes_MaximumJson_Passes()
    {
        var runtimeConfiguration = RuntimeConfiguration.FromBytes(
            """
            {
                "cache": "cache",
                "color": false,
                "git": "git",
                "github_proxy": "github_proxy",
                "go_module_proxy": "go_module_proxy",
                "https_proxy": "https_proxy",
                "noproxy": "noproxy",
                "proxy": "proxy",
                "script_shell": "script_shell"
            }
            """u8.ToArray()
        );

        Assert.Equal("cache", runtimeConfiguration.Cache);
        Assert.False(runtimeConfiguration.Color);
        Assert.Equal("git", runtimeConfiguration.Git);
        Assert.Equal("github_proxy", runtimeConfiguration.GitHubProxy);
        Assert.Equal("go_module_proxy", runtimeConfiguration.GoModuleProxy);
        Assert.Equal("https_proxy", runtimeConfiguration.HttpsProxy);
        Assert.Equal("noproxy", runtimeConfiguration.NoProxy);
        Assert.Equal("proxy", runtimeConfiguration.Proxy);
        Assert.Equal("script_shell", runtimeConfiguration.ScriptShell);
    }

    [Fact]
    public void FromBytes_NullJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("bytes", () => RuntimeConfiguration.FromBytes("null"u8.ToArray()));
    }

    [Fact]
    public void ToBytes_MinimumJson_Passes()
    {
        var runtimeConfiguration = new RuntimeConfiguration();

        Assert.Equal($$"""
            {
                "cache": "{{(OperatingSystem.IsWindows() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "lip-cache").Replace("\\", "\\\\") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "lip"))}}",
                "color": true,
                "git": "git",
                "github_proxy": "",
                "go_module_proxy": "https://goproxy.io",
                "https_proxy": "",
                "noproxy": "",
                "proxy": "",
                "script_shell": "{{(OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh")}}"
            }
            """.ReplaceLineEndings(), Encoding.UTF8.GetString(runtimeConfiguration.ToBytes()).ReplaceLineEndings());
    }
}
