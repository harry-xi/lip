using Flurl;
using Lip.Core.Infrastructure;
using Moq;
using System.IO.Abstractions;

namespace Lip.Core.RegressionTests.Infrastructure;

public class FileDownloaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private static IUserInteraction CreateNoOpUserInteraction()
    {
        Mock<IUserInteraction> mock = new();
        mock.Setup(u => u.RunWithProgress(It.IsAny<string>(), It.IsAny<Func<IProgress<double>, Task>>()))
            .Returns((string _, Func<IProgress<double>, Task> action) =>
                action(new Progress<double>()));
        return mock.Object;
    }

    [Fact]
    public async Task DownloadFile_ValidUrl_WritesFile()
    {
        FileDownloader downloader = new(CreateNoOpUserInteraction());
        FileSystem fileSystem = new();

        string tempPath = Path.Combine(_tempDir, Guid.NewGuid().ToString());

        IFileInfo destination = fileSystem.FileInfo.New(tempPath);

        // Use the Go module proxy version list endpoint, which the existing
        // regression tests already depend on.
        await downloader.DownloadFile(
            new Url("https://raw.githubusercontent.com/LiteLDev/LeviLamina/main/README.md"),
            destination);

        destination.Refresh();

        Assert.True(destination.Exists);
        Assert.True(destination.Length > 0);
    }

    [Fact]
    public async Task DownloadFile_NonExistentUrl_ThrowsHttpRequestException()
    {
        FileDownloader downloader = new(CreateNoOpUserInteraction());
        FileSystem fileSystem = new();

        string tempPath = Path.Combine(_tempDir, Guid.NewGuid().ToString());

        IFileInfo destination = fileSystem.FileInfo.New(tempPath);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => downloader.DownloadFile(
                new Url("https://raw.githubusercontent.com/LiteLDev/LeviLamina/main/THIS_FILE_DOES_NOT_EXIST.md"),
                destination));

        destination.Refresh();

        Assert.False(destination.Exists);
    }
}