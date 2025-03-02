using Flurl;

namespace Lip.Core;

public interface IDownloader
{
    Task DownloadFile(Url url, string destinationPath);
}
