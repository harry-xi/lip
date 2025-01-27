using System.IO.Abstractions.TestingHelpers;

namespace Lip.Tests;

public class StandaloneFileSourceTests
{
    [Fact]
    public async Task AddEntry_ThrowsNotImplementedException()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        var source = new StandaloneFileSource(fileSystem, filePath);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => source.AddEntry("key", new MemoryStream()));
    }

    [Fact]
    public async Task GetEntry_EmptyKey_ReturnsStandaloneFileSourceEntry()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        var source = new StandaloneFileSource(fileSystem, filePath);

        // Act
        IFileSourceEntry? entry = await source.GetEntry(string.Empty);

        // Assert
        Assert.NotNull(entry);
        Assert.Equal("Test content", new StreamReader(await entry.OpenEntryStream()).ReadToEnd());
    }

    [Fact]
    public async Task GetEntry_NonEmptyKey_ReturnsNull()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        var source = new StandaloneFileSource(fileSystem, filePath);

        // Act
        IFileSourceEntry? entry = await source.GetEntry("non-empty-key");

        // Assert
        Assert.Null(entry);
    }

    [Fact]
    public async Task RemoveEntry_ThrowsNotImplementedException()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        var source = new StandaloneFileSource(fileSystem, filePath);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => source.RemoveEntry("key"));
    }
}

public class StandaloneFileSourceEntryTests
{
    [Fact]
    public void IsDirectory_ReturnsFalse()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        var entry = new StandaloneFileSourceEntry(fileSystem, filePath);

        // Act
        bool isDirectory = entry.IsDirectory;

        // Assert
        Assert.False(isDirectory);
    }

    [Fact]
    public void Key_ReturnsEmptyString()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        var entry = new StandaloneFileSourceEntry(fileSystem, filePath);

        // Act
        string key = entry.Key;

        // Assert
        Assert.Equal(string.Empty, key);
    }

    [Fact]
    public async Task OpenEntryStream_ReturnsFileStream()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        var entry = new StandaloneFileSourceEntry(fileSystem, filePath);

        // Act
        using Stream stream = await entry.OpenEntryStream();

        // Assert
        Assert.Equal("Test content", new StreamReader(stream).ReadToEnd());
    }
}
