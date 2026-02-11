namespace Lip.Core.SourceProviders;

public interface ISourceProvider
{
    public IEnumerable<string> Keys { get; }

    public Task<Stream> OpenRead(string key);
}