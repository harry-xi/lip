using System.IO.Abstractions;

namespace Lip.Core.SourceProviders;

public class DirectorySourceProvider(IDirectoryInfo directoryInfo) : ISourceProvider
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
            ?? throw new ArgumentException($"Key not found: '{key}'");

        return fileInfo.OpenRead();
    }
}