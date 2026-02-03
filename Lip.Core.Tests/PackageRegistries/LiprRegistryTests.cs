using Flurl;
using Flurl.Http.Testing;
using Lip.Core.PackageRegistries;
using Semver;

namespace Lip.Core.Tests.PackageRegistries;

using static Lip.Core.PackageLock;

public class LiprRegistryTests
{
    private static PackageManifest CreateManifest(string toothPath = "example.com/pkg", string version = "1.0.0", List<PackageManifest.Variant>? variants = null)
    {
        return new PackageManifest
        {
            FormatVersion = DefaultFormatVersion,
            FormatUuid = DefaultFormatUuid,
            ToothPath = toothPath,
            Version = SemVersion.Parse(version),
            Info = new() { Name = "", Description = "", Tags = [], AvatarUrl = Url.Parse("https://example.com/icon") },
            Variants = variants ?? []
        };
    }

    private static readonly PackageManifest s_examplePackage_1 = CreateManifest(toothPath: "example.com/pkg", version: "1.0.0");

    [Fact]
    public async Task GetManifest_Success()
    {
        // Arrange.
        var expectedPackage = s_examplePackage_1;
        var packageSpecifier = new PackageSpecifier(
            new PackageIdentifier(expectedPackage.ToothPath, ""),
            expectedPackage.Version);

        // http://lipr.levimc.org/example.com/pkg/v1.0.0/tooth.json
        var expectedUrl = $"https://lipr.levimc.org/{expectedPackage.ToothPath}/v{expectedPackage.Version}/tooth.json";

        var liprRegistry = new LiprRegistry();

        // Act.
        using var httpTest = new HttpTest();
        httpTest.ForCallsTo(expectedUrl)
            .RespondWithJson(expectedPackage);

        var result = await liprRegistry.GetManifest(packageSpecifier);

        // Assert.
        Assert.Equal(LipTestExtensions.ToJsonBytes(expectedPackage), LipTestExtensions.ToJsonBytes(result));
    }

    [Fact]
    public async Task GetVersions_ThrowsNotImplemented()
    {
        // Arrange.
        var liprRegistry = new LiprRegistry();

        // Act & Assert.
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await liprRegistry.GetVersions(new PackageIdentifier("example.com/pkg", ""));
        });
    }
}