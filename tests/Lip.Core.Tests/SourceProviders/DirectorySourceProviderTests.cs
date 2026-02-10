using Lip.Core.SourceProviders;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.SourceProviders;

public class DirectorySourceProviderTests
{
    [Fact]
    public void Keys_ReturnsAllFiles()
    {
        // Arrange
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\test\file1.txt", new MockFileData("content1") },
            { @"C:\test\subdir\file2.txt", new MockFileData("content2") }
        });
        IDirectoryInfo dirInfo = mockFileSystem.DirectoryInfo.New(@"C:\test");
        DirectorySourceProvider provider = new(dirInfo);

        // Act
        List<string> keys = provider.Keys.ToList();

        // Assert
        Assert.Equal(2, keys.Count);
        Assert.Contains("file1.txt", keys);
        Assert.Contains(@"subdir\file2.txt", keys);
    }

    [Fact]
    public async Task OpenRead_ValidKey_ReturnsStream()
    {
        // Arrange
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\test\file.txt", new MockFileData("test content") }
        });
        IDirectoryInfo dirInfo = mockFileSystem.DirectoryInfo.New(@"C:\test");
        DirectorySourceProvider provider = new(dirInfo);

        // Act
        using Stream stream = await provider.OpenRead("file.txt");
        using StreamReader reader = new(stream);
        var content = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal("test content", content);
    }

    [Fact]
    public async Task OpenRead_InvalidKey_ThrowsArgumentException()
    {
        // Arrange
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\test\file.txt", new MockFileData("content") }
        });
        IDirectoryInfo dirInfo = mockFileSystem.DirectoryInfo.New(@"C:\test");
        DirectorySourceProvider provider = new(dirInfo);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenRead("nonexistent.txt"));
    }
}