using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.PackageRegistries;
using Semver;

namespace Lip.Core.RegressionTests.PackageRegistries;

public class GitPackageRegistryTests
{
    [Theory]
    [InlineData("github.com/LiteLDev/bds", 158)]
    [InlineData("github.com/LiteLDev/LegacyScriptEngine", 110)]
    [InlineData("github.com/LiteLDev/LeviLamina", 85)]
    [InlineData("github.com/LiteLDev/LeviLamina#client", 10)]
    public async Task GetAvailableVersions_ReturnsSortedMinCount(string packageId, int minVersionCount)
    {
        GitRunner gitRunner = new();
        GitPackageRegistry registry = new(gitRunner, githubProxy: null);

        IOrderedEnumerable<SemVersion> versions = await registry.GetAvailableVersions(PackageId.Parse(packageId));

        Assert.True(versions.Count() >= minVersionCount);
        Assert.Equal(versions, versions.Order(SemVersion.PrecedenceComparer));
    }
}