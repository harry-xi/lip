namespace Lip.Core.FileSources;

public interface IFileSource
{
    public IEnumerable<string> Keys { get; }

    public Task<Stream> OpenRead(string key);
}