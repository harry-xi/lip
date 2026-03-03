using Flurl;
using Lip.Core.Entities;

namespace Lip.Core.Tests.Entities;

public class RemotePackageSpecTests {
  [Fact]
  public void Constructor_ValidVariant_SetsProperties() {
    // Arrange
    Url url = new("https://example.com/package.zip");
    string variant = "valid_variant";

    // Act
    RemotePackageSpec spec = new(url, variant);

    // Assert
    Assert.Equal(url, spec.ArchiveUrl);
    Assert.Equal(variant, spec.Variant);
  }

  [Fact]
  public void Constructor_InvalidVariant_ThrowsFormatException() {
    // Arrange
    Url url = new("https://example.com/package.zip");
    string variant = "invalid-variant!";

    // Act & Assert
    Assert.Throws<FormatException>(() => new RemotePackageSpec(url, variant));
  }

  [Fact]
  public void ToString_ReturnsCorrectFormat() {
    // Arrange
    Url url = new("https://example.com/package.zip");
    string variant = "variant";
    RemotePackageSpec spec = new(url, variant);

    // Act
    string result = spec.ToString();

    // Assert
    Assert.Equal("https://example.com/package.zip#variant", result);
  }

  [Fact]
  public void Parse_ValidStringWithVariant_ReturnsRemotePackageSpec() {
    // Arrange
    string input = "https://example.com/package.zip#variant";

    // Act
    RemotePackageSpec result = RemotePackageSpec.Parse(input);

    // Assert
    Assert.Equal("https://example.com/package.zip", result.ArchiveUrl.ToString());
    Assert.Equal("variant", result.Variant);
  }

  [Fact]
  public void Parse_ValidStringWithoutVariant_ReturnsRemotePackageSpec() {
    // Arrange
    string input = "https://example.com/package.zip";

    // Act
    RemotePackageSpec result = RemotePackageSpec.Parse(input);

    // Assert
    Assert.Equal("https://example.com/package.zip", result.ArchiveUrl.ToString());
    Assert.Equal(string.Empty, result.Variant);
  }
}