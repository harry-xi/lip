using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace Lip.Core.SourceProviders;

public partial class GoModuleArchiveSourceProvider(IFileInfo archiveFileInfo)
    : ISourceProvider
{
    private readonly ArchiveSourceProvider _archiveSourceProvider = new(archiveFileInfo);

    public IEnumerable<string> Keys => _archiveSourceProvider.Keys
        .Where(k => KeyRegex().IsMatch(k))
        .Select(k => KeyRegex().Match(k).Groups["key"].Value);

    public async Task<Stream> OpenRead(string key)
    {
        string archiveKey = _archiveSourceProvider.Keys
            .FirstOrDefault(k => KeyRegex().IsMatch(k) && KeyRegex().Match(k).Groups["key"].Value == key)
            ?? throw new ArgumentException($"Key not found: {key}", nameof(key));

        return await _archiveSourceProvider.OpenRead(archiveKey);
    }

    [GeneratedRegex(@"^[^@]+@v[^/]+/(?<key>.+)$")]
    private static partial Regex KeyRegex();
}