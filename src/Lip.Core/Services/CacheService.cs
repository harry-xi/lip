using System.IO.Abstractions;

namespace Lip.Core.Services;

public interface ICacheService
{
    Task Clean();
    Task<IFileSystemInfo> GetOrCreate(
        string key,
        Func<IFileSystemInfo, Task> factory);
}