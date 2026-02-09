using Lip.Core.SourceProviders;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Lip.Core.Tests.SourceProviders;

public class DirectorySourceProviderTests
{
    [Fact]
    public void Keys_ReturnsAllFiles()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\test\file1.txt", new MockFileData("content1") },
            { @"C:\test\subdir\file2.txt", new MockFileData("content2") }
        });
        var dirInfo = mockFileSystem.DirectoryInfo.New(@"C:\test");
        var provider = new DirectorySourceProvider(dirInfo);

        // Act
        var keys = provider.Keys.ToList();

        // Assert
        Assert.Equal(2, keys.Count);
        Assert.Contains("file1.txt", keys);
        Assert.Contains(@"subdir\file2.txt", keys);
    }

    [Fact]
    public async Task OpenRead_ValidKey_ReturnsStream()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\test\file.txt", new MockFileData("test content") }
        });
        var dirInfo = mockFileSystem.DirectoryInfo.New(@"C:\test");
        var provider = new DirectorySourceProvider(dirInfo);

        // Act
        using var stream = await provider.OpenRead("file.txt");
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal("test content", content);
    }

    [Fact]
    public async Task OpenRead_InvalidKey_ThrowsArgumentException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\test\file.txt", new MockFileData("content") }
        });
        var dirInfo = mockFileSystem.DirectoryInfo.New(@"C:\test");
        var provider = new DirectorySourceProvider(dirInfo);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenRead("nonexistent.txt"));
    }
}