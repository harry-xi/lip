using Flurl;
using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.PackageRegistries;
using Moq;
using Semver;
using Xunit;

namespace Lip.Core.Tests.PackageRegistries;

public class GitPackageRegistryTests
{
    [Fact]
    public async Task GetAvailableVersions_ReturnsValidSemVerTags()
    {
        // Arrange
        var pkgId = new PackageId("github.com/test/pkg", "");
        var repoUrl = Url.Parse("https://github.com/test/pkg.git");

        var mockGitRunner = new Mock<IGitRunner>();
        var refs = new List<(string Sha, string Ref)>
        {
            ("hash1", "refs/tags/v1.0.0"),
            ("hash2", "refs/tags/v1.1.0-beta.1"),
            ("hash3", "refs/tags/v2.0.0"),
            ("hash4", "refs/tags/not-a-version"),
            ("hash5", "refs/heads/main")
        };

        mockGitRunner.Setup(r => r.LsRemote(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(refs);

        var registry = new GitPackageRegistry(mockGitRunner.Object, null);

        // Act
        var result = (await registry.GetAvailableVersions(pkgId)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(SemVersion.Parse("1.0.0"), result);
        Assert.Contains(SemVersion.Parse("1.1.0-beta.1"), result);
        Assert.Contains(SemVersion.Parse("2.0.0"), result);
    }

    [Fact]
    public async Task GetAvailableVersions_WithProxy_UsesProxyUrl()
    {
        // Arrange
        var pkgId = new PackageId("github.com/test/pkg", "");
        var proxyUrl = Url.Parse("https://proxy.com");
        var expectedRepoUrl = Url.Parse("https://proxy.com/test/pkg.git");

        var mockGitRunner = new Mock<IGitRunner>();
        mockGitRunner.Setup(r => r.LsRemote(It.Is<string>(u => u == expectedRepoUrl.ToString()), true, true))
            .ReturnsAsync([]);

        var registry = new GitPackageRegistry(mockGitRunner.Object, proxyUrl);

        // Act
        await registry.GetAvailableVersions(pkgId);

        // Assert
        mockGitRunner.Verify(r => r.LsRemote(expectedRepoUrl, true, true), Times.Once);
    }

    [Fact]
    public async Task GetPackageManifest_ThrowsNotSupportedException()
    {
        var mockGitRunner = new Mock<IGitRunner>();
        var registry = new GitPackageRegistry(mockGitRunner.Object, null);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            registry.GetPackageManifest(new PackageSpec(PackageId.Parse("github.com/test/repo"), new SemVersion(1, 0, 0))));
    }
}