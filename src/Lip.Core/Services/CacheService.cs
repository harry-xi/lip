using System.IO.Abstractions;

namespace Lip.Core.Services;

public interface ICacheService
{
    Task Clean();
    Task<IDirectoryInfo> GetOrCreateDirectory(
        string key,
        Func<IDirectoryInfo, Task> factory);
    Task<IFileInfo> GetOrCreateFile(
        string key,
        Func<IFileInfo, Task> factory);
}

public class CacheService(IFileSystem fileSystem) : ICacheService
{
    private static readonly string _cacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "lip", "cache");

    private readonly IFileSystem _fileSystem = fileSystem;

    public async Task Clean()
    {
        _fileSystem.Directory.Delete(_cacheDirectory, true);
    }

    public async Task<IDirectoryInfo> GetOrCreateDirectory(string key, Func<IDirectoryInfo, Task> factory)
    {
        string encodedKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key));

        string cachePath = Path.Combine(_cacheDirectory, encodedKey);

        IDirectoryInfo cacheInfo = _fileSystem.DirectoryInfo.New(cachePath);

        if (!cacheInfo.Exists)
        {
            await factory(cacheInfo);
        }

        return cacheInfo;
    }

    public async Task<IFileInfo> GetOrCreateFile(string key, Func<IFileInfo, Task> factory)
    {
        string encodedKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key));

        string cachePath = Path.Combine(_cacheDirectory, encodedKey);

        IFileInfo cacheInfo = _fileSystem.FileInfo.New(cachePath);

        if (!cacheInfo.Exists)
        {
            await factory(cacheInfo);
        }

        return cacheInfo;
    }
}