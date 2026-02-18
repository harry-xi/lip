using System.IO.Abstractions;

namespace Lip.Core.Sources;

public class DirectorySource(IDirectoryInfo directoryInfo) : ISource
{
    private readonly IDirectoryInfo _directoryInfo = directoryInfo;

    public IEnumerable<string> Keys => _directoryInfo
        .GetFiles("*", SearchOption.AllDirectories)
        .Select(f => Path.GetRelativePath(_directoryInfo.FullName, f.FullName));

    public async Task<Stream> OpenRead(string key)
    {
        IFileInfo fileInfo = _directoryInfo
            .GetFiles("*", SearchOption.AllDirectories)
            .FirstOrDefault(f => Path.GetRelativePath(_directoryInfo.FullName, f.FullName) == key)
            ?? throw new ArgumentException($"Key not found: '{key}'", nameof(key));

        return fileInfo.OpenRead();
    }
}