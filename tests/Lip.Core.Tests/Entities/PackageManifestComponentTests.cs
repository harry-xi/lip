using Lip.Core.Entities;
using Semver;
using Xunit;

namespace Lip.Core.Tests.Entities;

public class PackageManifestAssetPlacementTests
{
    [Fact]
    public void Constructor_ValidRelativeDst_Succeeds()
    {
        var placement = new PackageManifestAssetPlacement
        {
            Type = PackageManifestAssetPlacement.PlacementType.File,
            Src = "src/*",
            Dst = "plugins/test/"
        };

        Assert.Equal("plugins/test/", placement.Dst);
    }

    [Theory]
    [InlineData(@"C:\absolute\path")]
    [InlineData("/absolute/path")]
    [InlineData("../parent/path")]
    [InlineData("path/../escape")]
    public void Constructor_InvalidDst_ThrowsArgumentException(string dst)
    {
        Assert.Throws<ArgumentException>(() => new PackageManifestAssetPlacement
        {
            Type = PackageManifestAssetPlacement.PlacementType.File,
            Src = "src/*",
            Dst = dst
        });
    }

    [Fact]
    public void IsValidDst_RelativePath_ReturnsTrue()
    {
        Assert.True(PackageManifestAssetPlacement.IsValidDst("plugins/test/"));
        Assert.True(PackageManifestAssetPlacement.IsValidDst("file.txt"));
    }

    [Fact]
    public void IsValidDst_AbsoluteOrParentPath_ReturnsFalse()
    {
        Assert.False(PackageManifestAssetPlacement.IsValidDst(@"C:\path"));
        Assert.False(PackageManifestAssetPlacement.IsValidDst("../"));
    }
}

public class PackageReqtTests
{
    [Fact]
    public void Constructor_ValidValues_CreatesInstance()
    {
        var pkgId = new PackageId("github.com/test/pkg", "");
        var range = SemVersionRange.Parse(">=1.0.0");

        var reqt = new PackageReqt(pkgId, range);

        Assert.Equal(pkgId, reqt.Id);
        Assert.Equal(range, reqt.VersionRange);
    }
}

public class PackageManifestInfoTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsDefaults()
    {
        var info = new PackageManifestInfo();

        Assert.Equal("", info.Name);
        Assert.Equal("", info.Description);
        Assert.Empty(info.Tags);
        Assert.Null(info.AvatarUrl);
    }

    [Fact]
    public void Constructor_WithValues_SetsValues()
    {
        var info = new PackageManifestInfo
        {
            Name = "Test Package",
            Description = "A test package",
            Tags = ["test", "example"]
        };

        Assert.Equal("Test Package", info.Name);
        Assert.Equal("A test package", info.Description);
        Assert.Equal(2, info.Tags.Count);
    }
}

public class PackageManifestVariantTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsDefaults()
    {
        var variant = new PackageManifestVariant();

        Assert.Equal("", variant.Label);
        Assert.Equal("", variant.Platform);
        Assert.Empty(variant.Dependencies);
        Assert.Empty(variant.Assets);
        Assert.Empty(variant.PreserveFiles);
        Assert.Empty(variant.RemoveFiles);
        Assert.NotNull(variant.Scripts);
    }
}

public class PackageManifestAssetTests
{
    [Fact]
    public void Constructor_WithValues_SetsValues()
    {
        var asset = new PackageManifestAsset
        {
            Type = PackageManifestAsset.AssetType.Zip,
            Urls = [new Flurl.Url("https://example.com/file.zip")]
        };

        Assert.Equal(PackageManifestAsset.AssetType.Zip, asset.Type);
        Assert.Single(asset.Urls);
    }
}