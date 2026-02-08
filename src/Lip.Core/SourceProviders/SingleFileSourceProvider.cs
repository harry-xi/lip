using System.IO.Abstractions;

namespace Lip.Core.SourceProviders;

public class SingleFileSourceProvider(IFileInfo fileInfo) : ISourceProvider
{
    private readonly IFileInfo _fileInfo = fileInfo;

    public IEnumerable<string> Keys => [""];

    public async Task<Stream> OpenRead(string key)
    {
        if (key != "")
        {
            throw new ArgumentException($"Key not found: '{key}'");
        }

        return _fileInfo.OpenRead();
    }
}