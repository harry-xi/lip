using System.IO.Abstractions.TestingHelpers;

namespace Lip.Tests;

public class DirectoryFileSourceTests
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

        var source = new DirectoryFileSource(fileSystem, filePath);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => source.AddEntry("key", new MemoryStream()));
    }

    [Fact]
    public async Task GetEntry_PathExists_ReturnsDirectoryFileSourceEntry()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        var source = new DirectoryFileSource(fileSystem, filePath);

        // Act
        IFileSourceEntry? entry = await source.GetEntry(string.Empty);

        // Assert
        Assert.NotNull(entry);
        Assert.Equal("Test content", new StreamReader(await entry.OpenEntryStream()).ReadToEnd());
    }

    [Fact]
    public async Task GetEntry_PathDoesNotExist_ReturnsNull()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        var source = new DirectoryFileSource(fileSystem, filePath);

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

        var source = new DirectoryFileSource(fileSystem, filePath);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => source.RemoveEntry("key"));
    }
}

public class DirectoryFileSourceEntryTests
{
    [Fact]
    public void IsDirectory_IsFile_ReturnsFalse()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        string key = "file";

        var entry = new DirectoryFileSourceEntry(fileSystem, filePath, key);

        // Act
        bool isDirectory = entry.IsDirectory;

        // Assert
        Assert.False(isDirectory);
    }

    [Fact]
    public void IsDirectory_IsDirectory_ReturnsTrue()
    {
        // Arrange
        string dirPath = "/path/to/dir";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { dirPath, new MockDirectoryData() }
        });

        string key = "dir";

        var entry = new DirectoryFileSourceEntry(fileSystem, dirPath, key);

        // Act
        bool isDirectory = entry.IsDirectory;

        // Assert
        Assert.True(isDirectory);
    }

    [Fact]
    public void Key_ReturnsKey()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        string expectedKey = "key";

        var entry = new DirectoryFileSourceEntry(fileSystem, filePath, expectedKey);

        // Act
        string key = entry.Key;

        // Assert
        Assert.Equal("key", key);
    }

    [Fact]
    public async Task OpenEntryStream_IsFile_ReturnsFileStream()
    {
        // Arrange
        string filePath = "/path/to/file";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { filePath, new MockFileData("Test content") }
        });

        string key = "file";

        var entry = new DirectoryFileSourceEntry(fileSystem, filePath, key);

        // Act
        using Stream stream = await entry.OpenEntryStream();

        // Assert
        Assert.Equal("Test content", new StreamReader(stream).ReadToEnd());
    }

    [Fact]
    public async Task OpenEntryStream_IsDirectory_ThrowsNotSupportedException()
    {
        // Arrange
        string dirPath = "/path/to/dir";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { dirPath, new MockDirectoryData() }
        });

        string key = "dir";

        var entry = new DirectoryFileSourceEntry(fileSystem, dirPath, key);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => entry.OpenEntryStream());
    }
}
