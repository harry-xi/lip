using Lip.Core.Infrastructure;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.Infrastructure;

public class FileSystemExtensionsTests
{
    [Fact]
    public void CreateFileWithDirectory_DirectoryExists_CreatesFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\existing\placeholder.txt", new MockFileData("") }
        });

        // Act
        using var stream = mockFileSystem.CreateFileWithDirectory(@"C:\existing\newfile.txt");

        // Assert
        Assert.True(mockFileSystem.File.Exists(@"C:\existing\newfile.txt"));
    }

    [Fact]
    public void CreateFileWithDirectory_DirectoryNotExists_CreatesDirectoryAndFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\root\placeholder.txt", new MockFileData("") }
        });

        // Act
        using var stream = mockFileSystem.CreateFileWithDirectory(@"C:\root\newdir\newfile.txt");

        // Assert
        Assert.True(mockFileSystem.Directory.Exists(@"C:\root\newdir"));
        Assert.True(mockFileSystem.File.Exists(@"C:\root\newdir\newfile.txt"));
    }
}