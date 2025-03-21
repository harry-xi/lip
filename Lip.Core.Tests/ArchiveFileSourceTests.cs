using SharpCompress.Common;
using SharpCompress.Writers;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace Lip.Core.Tests;

public class ArchiveFileSourceTests
{
    [Theory]
    [InlineData(ArchiveType.Zip, CompressionType.None)]
    [InlineData(ArchiveType.Zip, CompressionType.Deflate)]
    [InlineData(ArchiveType.Zip, CompressionType.BZip2)]
    [InlineData(ArchiveType.Zip, CompressionType.LZMA)]
    [InlineData(ArchiveType.Zip, CompressionType.PPMd)]
    [InlineData(ArchiveType.Tar, CompressionType.None)]
    [InlineData(ArchiveType.Tar, CompressionType.GZip)]
    [InlineData(ArchiveType.Tar, CompressionType.LZip)]
    public void GetAllFiles_ReturnsFiles(ArchiveType archiveType, CompressionType compressionType)
    {
        // Arrange.
        MockFileSystem fileSystem = new();

        CreateTestFiles(
            fileSystem,
            archiveType,
            compressionType,
            new()
            {
                { "path/to/entry1", "test content 1" },
                { "path/to/entry2", "test content 2" }
            });

        ArchiveFileSource fileSource = new(fileSystem, "archive");

        // Act.
        IAsyncEnumerable<IFileSourceEntry> files = fileSource.GetAllEntries();

        // Assert.
        Assert.Collection(
            files.ToBlockingEnumerable(),
            async file =>
            {
                Assert.Equal("path/to/entry1", file.Key);
                Assert.Equal("test content 1", new StreamReader(await file.OpenRead()).ReadToEnd());
            },
            async file =>
            {
                Assert.Equal("path/to/entry2", file.Key);
                Assert.Equal("test content 2", new StreamReader(await file.OpenRead()).ReadToEnd());
            });
    }

    [Theory]
    [InlineData(ArchiveType.Zip, CompressionType.None)]
    [InlineData(ArchiveType.Zip, CompressionType.Deflate)]
    [InlineData(ArchiveType.Zip, CompressionType.BZip2)]
    [InlineData(ArchiveType.Zip, CompressionType.LZMA)]
    [InlineData(ArchiveType.Zip, CompressionType.PPMd)]
    [InlineData(ArchiveType.Tar, CompressionType.None)]
    [InlineData(ArchiveType.Tar, CompressionType.GZip)]
    [InlineData(ArchiveType.Tar, CompressionType.LZip)]
    public async Task GetFile_EntryExists_ReturnsFileStream(ArchiveType archiveType, CompressionType compressionType)
    {
        // Arrange.
        MockFileSystem fileSystem = new();

        CreateTestFiles(
            fileSystem,
            archiveType,
            compressionType,
            new()
            {
                { "path/to/entry1", "test content 1" },
                { "path/to/entry2", "test content 2" }
            });

        ArchiveFileSource fileSource = new(fileSystem, "archive");

        // Act.
        IFileSourceEntry? file = await fileSource.GetEntry("path/to/entry1");

        // Assert.
        Assert.NotNull(file);
        Assert.Equal("path/to/entry1", file.Key);
        Assert.Equal("test content 1", new StreamReader(await file.OpenRead()).ReadToEnd());
    }

    [Fact]
    public async Task GetFile_EntryDoesNotExist_ReturnsNull()
    {
        // Arrange.
        MockFileSystem fileSystem = new();

        CreateTestFiles(
            fileSystem,
            ArchiveType.Tar,
            CompressionType.None,
            new()
            {
                { "path/to/entry1", "test content 1" },
                { "path/to/entry2", "test content 2" }
            });

        ArchiveFileSource fileSource = new(fileSystem, "archive");

        // Act.
        IFileSourceEntry? file = await fileSource.GetEntry("path/to/entry3");

        // Assert.
        Assert.Null(file);
    }

    [Fact]
    public async Task GetFile_EmptyArchive_ReturnsNull()
    {
        // Arrange.
        MockFileSystem fileSystem = new();

        CreateTestFiles(fileSystem, ArchiveType.Tar, CompressionType.None, []);

        ArchiveFileSource fileSource = new(fileSystem, "archive");

        // Act.
        IFileSourceEntry? file = await fileSource.GetEntry("path/to/entry");

        // Assert.
        Assert.Null(file);
    }

    [Fact]
    public void Entry_Key_ReturnsKey()
    {
        // Arrange.
        ArchiveFileSourceEntry fileSourceEntry = new("path/to/entry", new MemoryStream());

        // Act.
        string key = fileSourceEntry.Key;

        // Assert.
        Assert.Equal("path/to/entry", key);
    }

    [Fact]
    public async Task Entry_OpenRead_ReturnsStream()
    {
        // Arrange.
        ArchiveFileSourceEntry fileSourceEntry = new("path/to/entry", new MemoryStream(Encoding.UTF8.GetBytes("test content")));

        // Act.
        Stream stream = await fileSourceEntry.OpenRead();

        // Assert.
        Assert.Equal("test content", new StreamReader(stream).ReadToEnd());
    }

    private static void CreateTestFiles(
        MockFileSystem fileSystem,
        ArchiveType archiveType,
        CompressionType compressionType,
        Dictionary<string, string> entries)
    {
        using FileSystemStream fileStream = fileSystem.File.Create("archive");

        using IWriter writer = WriterFactory.Open(fileStream, archiveType, new(compressionType));

        foreach (KeyValuePair<string, string> entry in entries)
        {
            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(entry.Value));
            writer.Write(entry.Key, stream);
        }
    }
}