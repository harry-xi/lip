using Flurl;
using Lip.Core.Tests;
using Semver;

namespace Lip.Core.Tests;

public partial class PackageManifestTests
{
    [Fact]
    public void InfoType_Constructor_ValidValues_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        List<string> tags = ["tag1", "tag2"];
        Url avatarUrl = new();

        PackageManifest.InfoType info = new()
        {
            Name = string.Empty,
            Description = string.Empty,
            Tags = tags,
            AvatarUrl = avatarUrl,
        };

        PackageManifest.InfoType newInfo = info with { };

        // Assert.
        Assert.Empty(newInfo.Name);
        Assert.Empty(newInfo.Description);
        Assert.Equal(tags, newInfo.Tags);
        Assert.Equal(avatarUrl, newInfo.AvatarUrl);
    }

    [Fact]
    public void InfoType_InvalidTags_ThrowsSchemaViolationException()
    {
        // Act & Assert.
        SchemaViolationException exception = Assert.Throws<SchemaViolationException>(
            () => new PackageManifest.InfoType { Tags = ["invalid.tag"] });

        Assert.Equal("info.tags[]", exception.Key);
    }

    [Theory]
    [InlineData("tag")]
    [InlineData("tag:subtag")]
    public void IsValidTag_CommonInput_ReturnsTrue(string tag)
    {
        Assert.True(PackageManifest.IsValidTag(tag));
    }

    [Theory]
    [InlineData("tag name")]
    [InlineData("tag!")]
    public void IsValidTag_InvalidInput_ReturnsFalse(string tag)
    {
        Assert.False(PackageManifest.IsValidTag(tag));
    }
}