namespace Lip.Core.RegressionTests.Installer;

public class InnoSetupTests {
  private static string RepoRoot =>
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

  private static string InnoScriptPath =>
    Path.Combine(RepoRoot, "inno", "lip.iss");

  private static string ReleaseWorkflowPath =>
    Path.Combine(RepoRoot, ".github", "workflows", "release.yml");

  [Fact]
  public void WindowsInstaller_RequiresDotNetRuntimePrerequisiteFlow() {
    string script = File.ReadAllText(InnoScriptPath);

    Assert.Contains("https://builds.dotnet.microsoft.com/dotnet/Runtime/{#DotNetRuntimeChannel}/latest.version", script);
    Assert.Contains("dotnet-runtime-", script);
    Assert.Contains("/install /quiet /norestart", script);
    Assert.Contains("Microsoft.NETCore.App", script);
    Assert.Contains("SOFTWARE\\dotnet\\Setup\\InstalledVersions\\{#DotNetRuntimeArch}", script);
    Assert.Contains("HKLM32", script);
    Assert.Contains("PrepareToInstall", script);
  }

  [Fact]
  public void InnoSetup_ProvidesDefaultDotNetRuntimeVersionDefines() {
    string script = File.ReadAllText(InnoScriptPath);

    Assert.Contains("#ifndef DotNetRuntimeChannel", script);
    Assert.Contains("#define DotNetRuntimeChannel \"10.0\"", script);
    Assert.Contains("#ifndef DotNetRuntimeMajor", script);
    Assert.Contains("#define DotNetRuntimeMajor \"10\"", script);
  }
}