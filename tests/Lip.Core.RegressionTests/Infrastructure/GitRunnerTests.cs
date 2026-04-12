using Lip.Core.Infrastructure;

namespace Lip.Core.RegressionTests.Infrastructure;

public class GitRunnerTests {
  [Theory]
  [InlineData("https://github.com/LiteLDev/MoreDimensions.git", 21)]
  [InlineData("https://github.com/LiteLDev/LegacyScriptEngine.git", 124)]
  [InlineData("https://github.com/LiteLDev/LeviLamina.git", 90)]
  public async Task LsRemote_ReturnsNonEmptyRefsWithTags(string repository, int minTagCount) {
    GitRunner runner = new();

    IEnumerable<(string Sha, string Ref)> results = await runner.LsRemote(
        repository, refs: true, tags: true);

    List<(string Sha, string Ref)> resultList = [.. results];

    Assert.True(resultList.Count >= minTagCount);
    Assert.All(resultList, r => {
      Assert.False(string.IsNullOrWhiteSpace(r.Sha));
      Assert.StartsWith("refs/tags/", r.Ref);
    });
  }

  [Theory]
  [InlineData("https://github.com/LiteLDev/MoreDimensions.git", "v1.13.0")]
  [InlineData("https://github.com/LiteLDev/LegacyScriptEngine.git", "v0.17.13")]
  [InlineData("https://github.com/LiteLDev/LeviLamina.git", "v1.9.9")]
  public async Task Clone_GitRepoDirExists(string repository, string branch) {
    GitRunner runner = new();

    string cloneDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());

    await runner.Clone(
        repository,
        dir: cloneDir,
        branch,
        depth: 1);

    Assert.True(Directory.Exists(Path.Combine(cloneDir, ".git")));
  }
}
