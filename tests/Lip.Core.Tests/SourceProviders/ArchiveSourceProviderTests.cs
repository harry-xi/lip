using Lip.Core.SourceProviders;
using Moq;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Xunit;

namespace Lip.Core.Tests.SourceProviders;

public class ArchiveSourceProviderTests
{
    [Fact]
    public async Task Keys_ReturnsAllFileKeys()
    {
        // Arrange
        using var memoryStream = new MemoryStream();

        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("file1.txt");
            using (var entryStream = entry.Open())
            {
                entryStream.Write(Encoding.UTF8.GetBytes("content1"));
            }

            var entry2 = archive.CreateEntry("dir/file2.txt");
            using (var entryStream2 = entry2.Open())
            {
                entryStream2.Write(Encoding.UTF8.GetBytes("content2"));
            }
        }
        memoryStream.Position = 0;
        byte[] archiveBytes = memoryStream.ToArray();

        var mockFileSystem = new MockFileSystem();
        var path = @"C:\archive.zip";
        mockFileSystem.AddFile(path, new MockFileData(archiveBytes));
        var fileInfo = mockFileSystem.FileInfo.New(path);

        var provider = new ArchiveSourceProvider(fileInfo);

        // Act
        var keys = provider.Keys.ToList();

        // Assert
        Assert.Equal(2, keys.Count);
        Assert.Contains("file1.txt", keys);
        Assert.Contains("dir/file2.txt", keys);
    }

    [Fact]
    public async Task OpenRead_ValidKey_ReturnsContent()
    {
        // Arrange
        using var memoryStream = new MemoryStream();

        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("file1.txt");
            using var entryStream = entry.Open();
            entryStream.Write(Encoding.UTF8.GetBytes("test content"));
        }
        memoryStream.Position = 0;
        byte[] archiveBytes = memoryStream.ToArray();

        var mockFileSystem = new MockFileSystem();
        var path = @"C:\archive.zip";
        mockFileSystem.AddFile(path, new MockFileData(archiveBytes));
        var fileInfo = mockFileSystem.FileInfo.New(path);

        var provider = new ArchiveSourceProvider(fileInfo);

        // Act
        using var stream = await provider.OpenRead("file1.txt");
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal("test content", content);
    }

    [Fact]
    public async Task OpenRead_InvalidKey_ThrowsArgumentException()
    {
        // Arrange
        using var memoryStream = new MemoryStream();

        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
        }
        memoryStream.Position = 0;
        byte[] archiveBytes = memoryStream.ToArray();

        var mockFileSystem = new MockFileSystem();
        var path = @"C:\archive.zip";
        mockFileSystem.AddFile(path, new MockFileData(archiveBytes));
        var fileInfo = mockFileSystem.FileInfo.New(path);

        var provider = new ArchiveSourceProvider(fileInfo);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenRead("nonexistent"));
    }
}