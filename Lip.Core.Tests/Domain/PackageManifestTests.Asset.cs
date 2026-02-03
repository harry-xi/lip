using Lip.Core.Tests;

namespace Lip.Core.Tests;

public partial class PackageManifestTests
{
    [Fact]
    public void Asset_Constructor_ValidValues_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        PackageManifest.Asset asset = new()
        {
            Type = PackageManifest.Asset.TypeEnum.Self,
            Urls = [],
            Placements = []
        };

        PackageManifest.Asset newAsset = asset with { };

        // Assert.
        Assert.Equal(PackageManifest.Asset.TypeEnum.Self, newAsset.Type);
        Assert.Empty(newAsset.Urls);
        Assert.Empty(newAsset.Placements);
    }
}