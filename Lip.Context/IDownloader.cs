namespace Lip.Context;

public interface IDownloader
{
    Task DownloadFile(Uri url, string destinationPath);
}
