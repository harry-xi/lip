using Lip.Core.SourceProviders;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace Lip.Core.Tests.SourceProviders;

public class GoModuleArchiveSourceProviderTests
{
    [Fact]
    public async Task Keys_FiltersAndExtractsKeys()
    {
        // Arrange
        // Go module archives typically have structure: module@version/path/to/file
        using var memoryStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("github.com/user/repo@v1.0.0/file1.txt");
            using (var entryStream = entry.Open())
            {
                entryStream.Write(Encoding.UTF8.GetBytes("c1"));
            }

            var entry2 = archive.CreateEntry("github.com/user/repo@v1.0.0/dir/file2.txt");
            using (var entryStream2 = entry2.Open())
            {
                entryStream2.Write(Encoding.UTF8.GetBytes("c2"));
            }

            var entry3 = archive.CreateEntry("other/file.txt");
            using (var entryStream3 = entry3.Open())
            {
                entryStream3.Write(Encoding.UTF8.GetBytes("c3"));
            }
        }
        memoryStream.Position = 0;
        byte[] archiveBytes = memoryStream.ToArray();

        var mockFileSystem = new MockFileSystem();
        var path = @"C:\archive.zip";
        mockFileSystem.AddFile(path, new MockFileData(archiveBytes));
        var fileInfo = mockFileSystem.FileInfo.New(path);

        var provider = new GoModuleArchiveSourceProvider(fileInfo);

        // Act
        var keys = provider.Keys.ToList();

        // Assert
        // keys should be relative to the module root
        Assert.Contains("file1.txt", keys);
        Assert.Contains("dir/file2.txt", keys);
        Assert.DoesNotContain("other/file.txt", keys);
    }

    [Fact]
    public async Task OpenRead_MapsKeyToArchiveEntry()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("github.com/user/repo@v1.0.0/file.txt");
            using var entryStream = entry.Open();
            entryStream.Write(Encoding.UTF8.GetBytes("content"));
        }
        memoryStream.Position = 0;
        byte[] archiveBytes = memoryStream.ToArray();

        var mockFileSystem = new MockFileSystem();
        var path = @"C:\archive.zip";
        mockFileSystem.AddFile(path, new MockFileData(archiveBytes));
        var fileInfo = mockFileSystem.FileInfo.New(path);

        var provider = new GoModuleArchiveSourceProvider(fileInfo);

        // Act
        using var stream = await provider.OpenRead("file.txt");
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal("content", content);
    }
}