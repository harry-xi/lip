using Semver;

namespace Lip.Core.Tests;

public class PackageSpecifierTests
{
    private const string _defaultText = "example.com/pkg#variant@1.0.0";
    private const string _defaultToothPath = "example.com/pkg";
    private const string _defaultVariantLabel = "variant";
    private readonly SemVersion _defaultVersion = new(1, 0, 0);

    [Fact]
    public void Constructor_ValidValues_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        PackageIdentifier identifier = new(_defaultToothPath, _defaultVariantLabel);
        PackageSpecifier specifier = new(identifier, _defaultVersion);

        PackageSpecifier newSpecifier = specifier with { };

        // Assert.
        Assert.Equal(_defaultToothPath, newSpecifier.ToothPath);
        Assert.Equal(_defaultVariantLabel, newSpecifier.VariantLabel);
        Assert.Equal(_defaultVersion, newSpecifier.Version);
    }

    [Fact]
    public void Constructor_InvalidToothPath_ThrowsArgumentException()
    {
        // Arrange & Act & Assert.
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new PackageSpecifier(new PackageIdentifier("invalid tooth path", _defaultVariantLabel), _defaultVersion));
        Assert.Equal("ToothPath", exception.ParamName);
    }

    [Fact]
    public void Constructor_InvalidVariantLabel_ThrowsArgumentException()
    {
        // Arrange & Act & Assert.
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new PackageSpecifier(new PackageIdentifier(_defaultToothPath, "invalid-variant"), _defaultVersion));
        Assert.Equal("VariantLabel", exception.ParamName);
    }

    [Fact]
    public void Identifier_ReturnsCorrectIdentifier()
    {
        // Arrange.
        PackageIdentifier expectedIdentifier = new(_defaultToothPath, _defaultVariantLabel);
        PackageSpecifier specifier = new(expectedIdentifier, _defaultVersion);

        // Act.
        PackageIdentifier identifier = specifier.Identifier;

        // Assert.
        Assert.Equal(expectedIdentifier, identifier);
    }

    [Fact]
    public void Parse_FullText_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        PackageSpecifier specifier = PackageSpecifier.Parse(_defaultText);

        // Assert.
        Assert.Equal(_defaultToothPath, specifier.ToothPath);
        Assert.Equal(_defaultVariantLabel, specifier.VariantLabel);
    }

    [Fact]
    public void Parse_WithoutVariantLabel_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        PackageSpecifier specifier = PackageSpecifier.Parse($"{_defaultToothPath}@{_defaultVersion}");

        // Assert.
        Assert.Equal(_defaultToothPath, specifier.ToothPath);
        Assert.Equal(string.Empty, specifier.VariantLabel);
    }

    [Fact]
    public void Parse_InvalidText_ThrowsFormatException()
    {
        // Arrange.
        string invalidSpecifier = "invalid specifier";

        // Act & Assert.
        Assert.Throws<FormatException>(() => PackageSpecifier.Parse(invalidSpecifier));
    }

    [Fact]
    public void ToString_WithVariantLabel_ReturnsCorrectText()
    {
        // Arrange.
        PackageSpecifier specifier = new(new PackageIdentifier(_defaultToothPath, _defaultVariantLabel), _defaultVersion);

        // Act.
        string specifierText = specifier.ToString();

        // Assert.
        Assert.Equal(_defaultText, specifierText);
    }

    [Fact]
    public void ToString_WithoutVariantLabel_ReturnsCorrectText()
    {
        // Arrange
        PackageSpecifier specifier = new(new PackageIdentifier(_defaultToothPath, string.Empty), _defaultVersion);

        // Act
        string specifierText = specifier.ToString();

        // Assert
        Assert.Equal($"{_defaultToothPath}@{_defaultVersion}", specifierText);
    }


    [Theory]
    [InlineData("example.com/pkg#variant@1.0.0")]
    [InlineData("example.com/pkg@2.0.0")]
    public void TryParse_ValidSpecifier_ReturnsTrue(string specifier)
    {
        Assert.True(PackageSpecifier.TryParse(specifier, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("example.com/pkg")]
    [InlineData("example.com/pkg#variant@invalid")]
    [InlineData("invalid//pkg#variant@1.0.0")]
    public void TryParse_InvalidSpecifier_ReturnsFalse(string specifier)
    {
        Assert.False(PackageSpecifier.TryParse(specifier, out _));
    }
}