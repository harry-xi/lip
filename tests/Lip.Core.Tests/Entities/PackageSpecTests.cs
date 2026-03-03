using Lip.Core.Entities;
using Semver;

namespace Lip.Core.Tests.Entities;

public class PackageSpecTests {
  [Fact]
  public void Constructor_SetsPropertiesCorrectly() {
    // Arrange
    PackageId packageId = new("github.com/user/repo", string.Empty);
    SemVersion version = new(1, 0, 0);

    // Act
    PackageSpec spec = new(packageId, version);

    // Assert
    Assert.Equal(packageId, spec.Id);
    Assert.Equal(version, spec.Version);
  }

  [Fact]
  public void ToString_ReturnsCorrectFormat() {
    // Arrange
    PackageId packageId = new("github.com/user/repo", string.Empty);
    SemVersion version = new(1, 0, 0);
    PackageSpec spec = new(packageId, version);

    // Act
    string result = spec.ToString();

    // Assert
    Assert.Equal("github.com/user/repo@1.0.0", result);
  }

  [Fact]
  public void Parse_ValidString_ReturnsPackageSpec() {
    // Arrange
    string input = "github.com/user/repo@1.0.0";

    // Act
    PackageSpec result = PackageSpec.Parse(input);

    // Assert
    Assert.Equal("github.com/user/repo", result.Id.Path);
    Assert.Equal(new SemVersion(1, 0, 0), result.Version);
  }

  [Fact]
  public void Parse_InvalidString_ThrowsFormatException() {
    // Arrange
    string input = "invalid-spec";

    // Act & Assert
    Assert.Throws<FormatException>(() => PackageSpec.Parse(input));
  }
}