namespace Lip.Core.SourceProviders;

public class CompositeSourceProvider(IEnumerable<ISourceProvider> providers) : ISourceProvider
{
    private readonly IEnumerable<ISourceProvider> _providers = providers;

    public IEnumerable<string> Keys => _providers
        .SelectMany(p => p.Keys)
        .Distinct();

    public async Task<Stream> OpenRead(string key)
    {
        foreach (ISourceProvider provider in _providers)
        {
            if (provider.Keys.Contains(key))
            {
                return await provider.OpenRead(key);
            }
        }

        throw new ArgumentException($"Key not found: {key}");
    }
}