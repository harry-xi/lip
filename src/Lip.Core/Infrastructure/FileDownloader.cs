using Flurl;
using System.IO.Abstractions;

namespace Lip.Core.Infrastructure;

public interface IFileDownloader
{
    Task DownloadFile(Url url, IFileInfo destination);
}