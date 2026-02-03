using Lip.Core.Tests;

namespace Lip.Core.Tests;

public partial class PackageManifestTests
{
    [Fact]
    public void Variant_Constructor_ValidValues_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        List<string> preserveFiles = ["path/to/preserve/file"];
        List<string> removeFiles = ["path/to/remove/file"];
        PackageManifest.ScriptsType scripts = new()
        {
            PreInstall = [],
            Install = [],
            PostInstall = [],
            PreUninstall = [],
            Uninstall = [],
            PostUninstall = [],

        };

        PackageManifest.Variant variant = new()
        {
            Label = string.Empty,
            Platform = string.Empty,
            Dependencies = [],
            Assets = [],
            PreserveFiles = preserveFiles,
            RemoveFiles = removeFiles,
            Scripts = scripts
        };

        PackageManifest.Variant newVariant = variant with { };

        // Assert.
        Assert.Empty(newVariant.Label);
        Assert.Empty(newVariant.Platform);
        Assert.Empty(newVariant.Dependencies);
        Assert.Empty(newVariant.Assets);
        Assert.Equal(preserveFiles, newVariant.PreserveFiles);
        Assert.Equal(removeFiles, newVariant.RemoveFiles);
        Assert.Equal(scripts, newVariant.Scripts);
    }

    [Fact]
    public void Variant_InvalidPreserveFiles_ThrowsSchemaViolationException()
    {
        // Act & Assert.
        SchemaViolationException exception = Assert.Throws<SchemaViolationException>(
            () => new PackageManifest.Variant { PreserveFiles = ["/invalid/file"] });

        Assert.Equal("variants[].preserve_files[]", exception.Key);
    }

    [Fact]
    public void Variant_InvalidRemoveFiles_ThrowsSchemaViolationException()
    {
        // Act & Assert.
        SchemaViolationException exception = Assert.Throws<SchemaViolationException>(
            () => new PackageManifest.Variant { RemoveFiles = ["/invalid/file"] });

        Assert.Equal("variants[].remove_files[]", exception.Key);
    }

    [Theory]
    [InlineData("label", "platform", "label", "platform", true)]
    [InlineData("*", "platform", "label", "platform", true)]
    [InlineData("", "platform", "label", "platform", false)]
    [InlineData("another_label", "platform", "label", "platform", false)]
    [InlineData("label", "*", "label", "platform", true)]
    [InlineData("label", "", "label", "platform", false)]
    [InlineData("label", "another_platform", "label", "platform", false)]
    public void Variant_Match_VariousValues_ReturnsCorrectAnswer(
        string label,
        string platform,
        string targetLabel,
        string targetPlatform,
        bool expectedAnswer)
    {
        // Arrange.
        PackageManifest.Variant variant = new()
        {
            Label = label,
            Platform = platform,
            Dependencies = [],
            Assets = [],
            PreserveFiles = ["path/to/preserve/file"],
            RemoveFiles = ["path/to/remove/file"],
            Scripts = new()
            {
                PreInstall = [],
                Install = [],
                PostInstall = [],
                PreUninstall = [],
                Uninstall = [],
                PostUninstall = [],

            }
        };

        // Act.
        bool answer = variant.Match(targetLabel, targetPlatform);

        // Assert.
        Assert.Equal(expectedAnswer, answer);
    }
}