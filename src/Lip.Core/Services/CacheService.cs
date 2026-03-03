using System.IO.Abstractions;
using Lip.Core.Infrastructure;

namespace Lip.Core.Services;

public interface ICacheService {
  Task Clean();
  Task<IDirectoryInfo> GetOrCreateDirectory(
      string key,
      Func<IDirectoryInfo, Task> factory);
  Task<IFileInfo> GetOrCreateFile(
      string key,
      Func<IFileInfo, Task> factory);
}

public class CacheService(IFileSystem fileSystem, IUserInteraction userInteraction) : ICacheService {
  private readonly IDirectoryInfo _cacheDirectory = fileSystem.DirectoryInfo.New(
      Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
          "lip",
          "cache"));

  private readonly IFileSystem _fileSystem = fileSystem;
  private readonly IUserInteraction _userInteraction = userInteraction;

  public async Task Clean() {
    if (!_cacheDirectory.Exists) {
      await _userInteraction.PrintWarning(
          $"Cache directory '{_cacheDirectory.FullName}' does not exist, skipping clean.");

      return;
    }

    _cacheDirectory.Delete(recursive: true);
  }

  public async Task<IDirectoryInfo> GetOrCreateDirectory(string key, Func<IDirectoryInfo, Task> factory) {
    string safeKey = Uri.EscapeDataString(key);

    IDirectoryInfo cacheInfo = _fileSystem.DirectoryInfo.New(Path.Combine(
        _cacheDirectory.FullName, safeKey));

    if (!cacheInfo.Exists) {
      _fileSystem.Directory.CreateDirectory(cacheInfo.FullName);

      await factory(cacheInfo);
    }

    return cacheInfo;
  }

  public async Task<IFileInfo> GetOrCreateFile(string key, Func<IFileInfo, Task> factory) {
    string safeKey = Uri.EscapeDataString(key);

    IFileInfo cacheInfo = _fileSystem.FileInfo.New(Path.Combine(
        _cacheDirectory.FullName, safeKey));

    if (!cacheInfo.Exists) {
      _fileSystem.CreateFileWithDirectory(cacheInfo.FullName).Dispose();

      await factory(cacheInfo);
    }

    return cacheInfo;
  }
}