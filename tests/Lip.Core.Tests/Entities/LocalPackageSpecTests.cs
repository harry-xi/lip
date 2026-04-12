using System.IO.Abstractions.TestingHelpers;
using Lip.Core.Entities;

namespace Lip.Core.Tests.Entities;

public class LocalPackageSpecTests {
  [Fact]
  public void Parse_ValidPath_ReturnsSpec() {
    // Arrange
    string root = Path.GetPathRoot(Environment.CurrentDirectory) ?? "/";
    string packagePath = Path.Combine(root, "path", "to", "package.zip");
    MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
    {
            { packagePath, new MockFileData("content") }
        });

    // Act
    LocalPackageSpec spec = LocalPackageSpec.Parse(packagePath, mockFileSystem);

    // Assert
    Assert.Equal(packagePath, spec.ArchiveFile.FullName);
    Assert.Equal(string.Empty, spec.Variant);
  }

  [Fact]
  public void Parse_ValidPathWithVariant_ReturnsSpec() {
    // Arrange
    string root = Path.GetPathRoot(Environment.CurrentDirectory) ?? "/";
    string packagePath = Path.Combine(root, "path", "to", "package.zip");
    MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
    {
            { packagePath, new MockFileData("content") }
        });

    // Act
    LocalPackageSpec spec = LocalPackageSpec.Parse($"{packagePath}#variant", mockFileSystem);

    // Assert
    Assert.Equal(packagePath, spec.ArchiveFile.FullName);
    Assert.Equal("variant", spec.Variant);
  }

  [Fact]
  public void Parse_FileNotFound_ThrowsFileNotFoundException() {
    // Arrange
    MockFileSystem mockFileSystem = new();
    string root = Path.GetPathRoot(Environment.CurrentDirectory) ?? "/";
    string nonexistentPath = Path.Combine(root, "nonexistent.zip");

    // Act & Assert
    Assert.Throws<FileNotFoundException>(() => LocalPackageSpec.Parse(nonexistentPath, mockFileSystem));
  }

  [Fact]
  public void Parse_InvalidVariant_ThrowsFormatException() {
    // Arrange
    string root = Path.GetPathRoot(Environment.CurrentDirectory) ?? "/";
    string packagePath = Path.Combine(root, "path", "to", "package.zip");
    MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
    {
            { packagePath, new MockFileData("content") }
        });

    // Act & Assert
    Assert.Throws<FormatException>(() => LocalPackageSpec.Parse($"{packagePath}#invalid-variant!", mockFileSystem));
  }
}
