namespace Lip.Core.FileSources;

public interface IFileSource
{
    public IEnumerable<string> Keys { get; }

    public Task<Stream> this[string key] { get; }
}