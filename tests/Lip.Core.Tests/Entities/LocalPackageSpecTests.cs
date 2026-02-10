using Lip.Core.Entities;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.Entities;

public class LocalPackageSpecTests
{
    [Fact]
    public void Parse_ValidPath_ReturnsSpec()
    {
        // Arrange
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\path\to\package.zip", new MockFileData("content") }
        });

        // Act
        LocalPackageSpec spec = LocalPackageSpec.Parse(@"C:\path\to\package.zip", mockFileSystem);

        // Assert
        Assert.Equal(@"C:\path\to\package.zip", spec.ArchiveFile.FullName);
        Assert.Equal(string.Empty, spec.Variant);
    }

    [Fact]
    public void Parse_ValidPathWithVariant_ReturnsSpec()
    {
        // Arrange
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\path\to\package.zip", new MockFileData("content") }
        });

        // Act
        LocalPackageSpec spec = LocalPackageSpec.Parse(@"C:\path\to\package.zip#variant", mockFileSystem);

        // Assert
        Assert.Equal(@"C:\path\to\package.zip", spec.ArchiveFile.FullName);
        Assert.Equal("variant", spec.Variant);
    }

    [Fact]
    public void Parse_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        MockFileSystem mockFileSystem = new();

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => LocalPackageSpec.Parse(@"C:\nonexistent.zip", mockFileSystem));
    }

    [Fact]
    public void Parse_InvalidVariant_ThrowsFormatException()
    {
        // Arrange
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\path\to\package.zip", new MockFileData("content") }
        });

        // Act & Assert
        Assert.Throws<FormatException>(() => LocalPackageSpec.Parse(@"C:\path\to\package.zip#invalid-variant!", mockFileSystem));
    }
}