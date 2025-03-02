using Flurl;
using Flurl.Http;
using Lip.Core;

namespace Lip.Context;

public class Downloader : IDownloader
{
    public async Task DownloadFile(Url url, string destinationPath)
    {
        using Stream downloadStream = await url.GetStreamAsync();
        using Stream fileStream = File.Create(destinationPath);

        await downloadStream.CopyToAsync(fileStream);
    }
}
