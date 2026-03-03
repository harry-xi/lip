namespace Lip.Core.Sources;

public interface ISource {
  public IEnumerable<string> Keys { get; }

  public Task<Stream> OpenRead(string key);
}