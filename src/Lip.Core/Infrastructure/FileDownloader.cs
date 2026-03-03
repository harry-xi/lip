using System.IO.Abstractions;
using Downloader;
using Flurl;
using Flurl.Http;

namespace Lip.Core.Infrastructure;

public interface IFileDownloader {
  Task DownloadFile(Url url, IFileInfo destination);
}

public class FileDownloader(IUserInteraction userInteraction) : IFileDownloader {
  private readonly IUserInteraction _userInteraction = userInteraction;

  public async Task DownloadFile(Url url, IFileInfo destination) {
    IFlurlResponse response = await url.AllowAnyHttpStatus().GetAsync();

    if (response.StatusCode != 200) {
      throw new HttpRequestException($"Failed to download {url}: HTTP {response.StatusCode}");
    }

    await _userInteraction.RunWithProgress($"Downloading {url}", async progress => {
      await using IDownload downloader = DownloadBuilder.New()
          .WithUrl(url)
          .WithFileLocation(destination.FullName)
          .WithConfiguration(new() {
            ChunkCount = 4,
            ParallelDownload = true,
            RequestConfiguration =
              {
                        Proxy = HttpClient.DefaultProxy,
              }
          })
          .Build();

      downloader.DownloadProgressChanged += (sender, e) => {
        progress.Report(e.ProgressPercentage);
      };

      downloader.DownloadFileCompleted += (sender, e) => {
        if (e.Error is not null) {
          throw e.Error;
        }
      };

      await downloader.StartAsync();
    });
  }
}