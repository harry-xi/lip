namespace Lip.Core.Tests;

public class PackageIdentifierTests
{
    private const string _defaultText = "example.com/pkg#variant";
    private const string _defaultToothPath = "example.com/pkg";
    private const string _defaultVariantLabel = "variant";

    [Fact]
    public void Constructor_ValidValues_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        PackageIdentifier identifier = new(_defaultToothPath, _defaultVariantLabel);

        PackageIdentifier newIdentifier = identifier with { };

        // Assert.
        Assert.Equal(_defaultToothPath, newIdentifier.ToothPath);
        Assert.Equal(_defaultVariantLabel, newIdentifier.VariantLabel);
    }

    [Fact]
    public void Constructor_InvalidToothPath_ThrowsArgumentException()
    {
        // Arrange & Act & Assert.
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PackageIdentifier("invalid tooth path", _defaultVariantLabel));
        Assert.Equal("ToothPath", exception.ParamName);
    }

    [Fact]
    public void Constructor_InvalidVariantLabel_ThrowsArgumentException()
    {
        // Arrange & Act & Assert.
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PackageIdentifier(_defaultToothPath, "invalid-variant"));
        Assert.Equal("VariantLabel", exception.ParamName);
    }

    [Fact]
    public void Parse_FullText_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        PackageIdentifier identifier = PackageIdentifier.Parse(_defaultText);

        // Assert.
        Assert.Equal(_defaultToothPath, identifier.ToothPath);
        Assert.Equal(_defaultVariantLabel, identifier.VariantLabel);
    }

    [Fact]
    public void Parse_WithoutVariantLabel_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        PackageIdentifier identifier = PackageIdentifier.Parse(_defaultToothPath);

        // Assert.
        Assert.Equal(_defaultToothPath, identifier.ToothPath);
        Assert.Equal(string.Empty, identifier.VariantLabel);
    }

    [Fact]
    public void Parse_InvalidText_ThrowsFormatException()
    {
        // Arrange.
        string text = "invalid text";

        // Act & Assert.
        Assert.Throws<FormatException>(() => PackageIdentifier.Parse(text));
    }

    [Fact]
    public void ToString_WithVariantLabel_ReturnsCorrectText()
    {
        // Arrange.
        PackageIdentifier identifier = new(_defaultToothPath, _defaultVariantLabel);

        // Act.
        string text = identifier.ToString();

        // Assert.
        Assert.Equal(_defaultText, text);
    }

    [Fact]
    public void ToString_WithoutVariantLabel_ReturnsCorrectText()
    {
        // Arrange
        PackageIdentifier identifier = new(_defaultToothPath, string.Empty);

        // Act
        string text = identifier.ToString();

        // Assert
        Assert.Equal(_defaultToothPath, text);
    }


    [Theory]
    [InlineData("example.com/pkg")]
    [InlineData("example.com/pkg#variant")]
    public void TryParse_ValidIdentifier_ReturnsTrue(string identifier)
    {
        Assert.True(PackageIdentifier.TryParse(identifier, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("example.com//pkg")]
    [InlineData("example.com/pkg#invalid!variant")]
    [InlineData("example.com/pkg#invalid#variant")]
    public void TryParse_InvalidIdentifier_ReturnsFalse(string identifier)
    {
        Assert.False(PackageIdentifier.TryParse(identifier, out _));
    }

    [Theory]
    [InlineData("example123.example-domain/example-pkg.example_pkg~Example123")]
    [InlineData("example.com/~a12")]
    [InlineData("github.com/user/repo")]
    public void IsValidToothPath_ValidPath_ReturnsTrue(string path)
    {
        Assert.True(PackageIdentifier.IsValidToothPath(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("-example.com/pkg")]
    [InlineData("example.com//pkg")]
    [InlineData("example.com/pkg/")]
    [InlineData("example/pkg")]
    [InlineData("Example.com/pkg")]
    [InlineData("example\0.com/pkg")]
    [InlineData("example.com/../pkg")]
    [InlineData("example.com/.pkg")]
    [InlineData("example.com/pkg.")]
    [InlineData("example.com/p*kg")]
    [InlineData("example.com/con.pkg")]
    [InlineData("example.com/pkg~123")]
    public void IsValidToothPath_InvalidPath_ReturnsFalse(string path)
    {
        Assert.False(PackageIdentifier.IsValidToothPath(path));
    }

    [Theory]
    [InlineData("variant")]
    [InlineData("variant_name")]
    public void IsValidVariantLabel_ValidLabel_ReturnsTrue(string label)
    {
        Assert.True(PackageIdentifier.IsValidVariantLabel(label));
    }

    [Theory]
    [InlineData("invalid-variant")]
    [InlineData("invalid!variant")]
    public void IsValidVariantLabel_InvalidLabel_ReturnsFalse(string label)
    {
        Assert.False(PackageIdentifier.IsValidVariantLabel(label));
    }
}