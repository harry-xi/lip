using Flurl;
using Lip.Core.Infrastructure;
using Moq;
using System.IO.Abstractions;

namespace Lip.Core.RegressionTests.Infrastructure;

public class FileDownloaderTests
{
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

        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        await downloader.DownloadFile(
            new Url("https://raw.githubusercontent.com/LiteLDev/LeviLamina/main/README.md"),
            new FileSystem().FileInfo.New(tempPath));

        Assert.True(File.Exists(tempPath));
        Assert.True(new FileInfo(tempPath).Length > 0);
    }

    [Fact]
    public async Task DownloadFile_NonExistentUrl_ThrowsHttpRequestException()
    {
        FileDownloader downloader = new(CreateNoOpUserInteraction());

        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        await Assert.ThrowsAsync<HttpRequestException>(
            () => downloader.DownloadFile(
                new Url("https://raw.githubusercontent.com/LiteLDev/LeviLamina/main/THIS_FILE_DOES_NOT_EXIST.md"),
                new FileSystem().FileInfo.New(tempPath)));

        Assert.False(File.Exists(tempPath));
    }
}