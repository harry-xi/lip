namespace Lip.Core.Sources;

public class CompositeSource(IEnumerable<ISource> providers) : ISource {
  private readonly IEnumerable<ISource> _providers = providers;

  public IEnumerable<string> Keys => _providers
      .SelectMany(p => p.Keys)
      .Distinct();

  public async Task<Stream> OpenRead(string key) {
    foreach (ISource provider in _providers) {
      if (provider.Keys.Contains(key)) {
        return await provider.OpenRead(key);
      }
    }

    throw new ArgumentException($"Key not found: {key}", nameof(key));
  }
}
