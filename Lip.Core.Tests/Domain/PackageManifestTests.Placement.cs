namespace Lip.Core.Tests;

public partial class PackageManifestTests
{
    [Fact]
    public void Placement_Constructor_ValidValues_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        string dest = "path/to/dest";

        PackageManifest.Placement placement = new()
        {
            Type = PackageManifest.Placement.TypeEnum.File,
            Src = string.Empty,
            Dest = dest,
        };

        PackageManifest.Placement newPlacement = placement with { };

        // Assert.
        Assert.Equal(PackageManifest.Placement.TypeEnum.File, newPlacement.Type);
        Assert.Empty(newPlacement.Src);
        Assert.Equal(dest, newPlacement.Dest);
    }

    [Fact]
    public void Placement_InvalidDest_ThrowsSchemaViolationException()
    {
        // Act & Assert.
        SchemaViolationException exception = Assert.Throws<SchemaViolationException>(
            () => new PackageManifest.Placement
            {
                Type = PackageManifest.Placement.TypeEnum.File,
                Src = string.Empty,
                Dest = "/invalid/dest"
            });

        Assert.Equal("placements[].dest", exception.Key);
    }

    [Theory]
    [InlineData("folder/subfolder")]
    [InlineData("path")]
    public void IsValidPlacementDest_SafePath_ReturnsTrue(string path)
    {
        Assert.True(PackageManifest.IsValidPlacementDest(path));
    }

    [Fact]
    public void IsValidPlacementDest_PathWithDoubleDots_ReturnsFalse()
    {
        Assert.False(PackageManifest.IsValidPlacementDest("folder/../escape"));
    }

    [Fact]
    public void IsValidPlacementDest_PathWithRoot_ReturnsFalse()
    {
        string path = OperatingSystem.IsWindows() ? "C:\\root" : "/root";
        Assert.False(PackageManifest.IsValidPlacementDest(path));
    }
}