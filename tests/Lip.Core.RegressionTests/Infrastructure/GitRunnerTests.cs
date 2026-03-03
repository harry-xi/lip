using Lip.Core.Infrastructure;

namespace Lip.Core.RegressionTests.Infrastructure;

public class GitRunnerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("https://github.com/LiteLDev/bds.git", 158)]
    [InlineData("https://github.com/LiteLDev/LegacyScriptEngine.git", 110)]
    [InlineData("https://github.com/LiteLDev/LeviLamina.git", 85)]
    public async Task LsRemote_ReturnsNonEmptyRefsWithTags(string repository, int minTagCount)
    {
        GitRunner runner = new();

        IEnumerable<(string Sha, string Ref)> results = await runner.LsRemote(
            repository, refs: true, tags: true);

        List<(string Sha, string Ref)> resultList = [.. results];

        Assert.True(resultList.Count >= minTagCount);
        Assert.All(resultList, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Sha));
            Assert.StartsWith("refs/tags/", r.Ref);
        });
    }

    [Theory]
    [InlineData("https://github.com/LiteLDev/bds.git", "v1.26.3")]
    [InlineData("https://github.com/LiteLDev/LegacyScriptEngine.git", "v0.17.5")]
    [InlineData("https://github.com/LiteLDev/LeviLamina.git", "v1.9.7")]
    public async Task Clone_GitRepoDirExists(string repository, string branch)
    {
        GitRunner runner = new();

        string cloneDir = Path.Join(_tempDir, Guid.NewGuid().ToString());

        await runner.Clone(
            repository,
            dir: cloneDir,
            branch,
            depth: 1);

        Assert.True(Directory.Exists(Path.Combine(cloneDir, ".git")));
    }
}