using Flurl;
using Lip.Core.Entities;
using Xunit;

namespace Lip.Core.Tests.Entities;

public class RemotePackageSpecTests
{
    [Fact]
    public void Constructor_ValidVariant_SetsProperties()
    {
        // Arrange
        var url = new Url("https://example.com/package.zip");
        var variant = "valid_variant";

        // Act
        var spec = new RemotePackageSpec(url, variant);

        // Assert
        Assert.Equal(url, spec.ArchiveUrl);
        Assert.Equal(variant, spec.Variant);
    }

    [Fact]
    public void Constructor_InvalidVariant_ThrowsFormatException()
    {
        // Arrange
        var url = new Url("https://example.com/package.zip");
        var variant = "invalid-variant!";

        // Act & Assert
        Assert.Throws<FormatException>(() => new RemotePackageSpec(url, variant));
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var url = new Url("https://example.com/package.zip");
        var variant = "variant";
        var spec = new RemotePackageSpec(url, variant);

        // Act
        var result = spec.ToString();

        // Assert
        Assert.Equal("https://example.com/package.zip#variant", result);
    }

    [Fact]
    public void Parse_ValidStringWithVariant_ReturnsRemotePackageSpec()
    {
        // Arrange
        var input = "https://example.com/package.zip#variant";

        // Act
        var result = RemotePackageSpec.Parse(input);

        // Assert
        Assert.Equal("https://example.com/package.zip", result.ArchiveUrl.ToString());
        Assert.Equal("variant", result.Variant);
    }

    [Fact]
    public void Parse_ValidStringWithoutVariant_ReturnsRemotePackageSpec()
    {
        // Arrange
        var input = "https://example.com/package.zip";

        // Act
        var result = RemotePackageSpec.Parse(input);

        // Assert
        Assert.Equal("https://example.com/package.zip", result.ArchiveUrl.ToString());
        Assert.Equal(string.Empty, result.Variant);
    }
}