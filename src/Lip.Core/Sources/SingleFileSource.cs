using System.IO.Abstractions;

namespace Lip.Core.Sources;

public class SingleFileSource(IFileInfo fileInfo) : ISource
{
    private readonly IFileInfo _fileInfo = fileInfo;

    public IEnumerable<string> Keys => [""];

    public async Task<Stream> OpenRead(string key)
    {
        if (key != "")
        {
            throw new ArgumentException($"Key not found: '{key}'", nameof(key));
        }

        return _fileInfo.OpenRead();
    }
}