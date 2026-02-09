using Lip.Core.Entities;
using Semver;
using Xunit;

namespace Lip.Core.Tests.Entities;

public class PackageSpecTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var packageId = new PackageId("github.com/user/repo", string.Empty);
        var version = new SemVersion(1, 0, 0);

        // Act
        var spec = new PackageSpec(packageId, version);

        // Assert
        Assert.Equal(packageId, spec.Id);
        Assert.Equal(version, spec.Version);
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var packageId = new PackageId("github.com/user/repo", string.Empty);
        var version = new SemVersion(1, 0, 0);
        var spec = new PackageSpec(packageId, version);

        // Act
        var result = spec.ToString();

        // Assert
        Assert.Equal("github.com/user/repo@1.0.0", result);
    }

    [Fact]
    public void Parse_ValidString_ReturnsPackageSpec()
    {
        // Arrange
        var input = "github.com/user/repo@1.0.0";

        // Act
        var result = PackageSpec.Parse(input);

        // Assert
        Assert.Equal("github.com/user/repo", result.Id.Path);
        Assert.Equal(new SemVersion(1, 0, 0), result.Version);
    }

    [Fact]
    public void Parse_InvalidString_ThrowsFormatException()
    {
        // Arrange
        var input = "invalid-spec";

        // Act & Assert
        Assert.Throws<FormatException>(() => PackageSpec.Parse(input));
    }
}