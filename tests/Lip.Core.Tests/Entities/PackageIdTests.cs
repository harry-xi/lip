using Lip.Core.Entities;

namespace Lip.Core.Tests.Entities;

public class PackageIdTests
{
    [Theory]
    [InlineData("github.com/user/repo", "")]
    [InlineData("github.com/user/pkg", "win_x64")]
    public void Constructor_ValidInputs_CreatesInstance(string path, string variant)
    {
        // Act
        var packageId = new PackageId(path, variant);

        // Assert
        Assert.Equal(path, packageId.Path);
        Assert.Equal(variant, packageId.Variant);
    }

    [Theory]
    [InlineData("Invalid Path")]
    [InlineData("")]
    public void Constructor_InvalidPath_ThrowsFormatException(string path)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => new PackageId(path, ""));
    }

    [Theory]
    [InlineData("Invalid-Variant!")]
    [InlineData("UPPER")]
    public void Constructor_InvalidVariant_ThrowsFormatException(string variant)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => new PackageId("github.com/user/repo", variant));
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var packageId = new PackageId("github.com/user/repo", "variant");

        // Act
        var result = packageId.ToString();

        // Assert
        Assert.Equal("github.com/user/repo#variant", result);
    }

    [Fact]
    public void Parse_ValidString_ReturnsPackageId()
    {
        // Arrange
        var input = "github.com/user/repo#variant";

        // Act
        var result = PackageId.Parse(input);

        // Assert
        Assert.Equal("github.com/user/repo", result.Path);
        Assert.Equal("variant", result.Variant);
    }

    [Fact]
    public void Parse_InvalidString_ThrowsFormatException()
    {
        // Arrange - starts with # which makes path empty
        var input = "#invalid";

        // Act & Assert
        Assert.Throws<FormatException>(() => PackageId.Parse(input));
    }
}