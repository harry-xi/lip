using Lip.Core.SourceProviders;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Lip.Core.Tests.SourceProviders;

public class SingleFileSourceProviderTests
{
    [Fact]
    public void Keys_ReturnsEmptyStringKey()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\test\file.txt", new MockFileData("content") }
        });
        var fileInfo = mockFileSystem.FileInfo.New(@"C:\test\file.txt");
        var provider = new SingleFileSourceProvider(fileInfo);

        // Act
        var keys = provider.Keys.ToList();

        // Assert
        Assert.Single(keys);
        Assert.Equal("", keys[0]);
    }

    [Fact]
    public async Task OpenRead_EmptyKey_ReturnsStream()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\test\file.txt", new MockFileData("file content") }
        });
        var fileInfo = mockFileSystem.FileInfo.New(@"C:\test\file.txt");
        var provider = new SingleFileSourceProvider(fileInfo);

        // Act
        using var stream = await provider.OpenRead("");
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal("file content", content);
    }

    [Fact]
    public async Task OpenRead_NonEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\test\file.txt", new MockFileData("content") }
        });
        var fileInfo = mockFileSystem.FileInfo.New(@"C:\test\file.txt");
        var provider = new SingleFileSourceProvider(fileInfo);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenRead("anykey"));
    }
}