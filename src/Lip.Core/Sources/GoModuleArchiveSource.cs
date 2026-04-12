using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace Lip.Core.Sources;

public partial class GoModuleArchiveSource(IFileInfo archiveFileInfo)
    : ISource {
  private readonly ArchiveSource _archiveSource = new(archiveFileInfo);

  public IEnumerable<string> Keys => _archiveSource.Keys
      .Where(k => KeyRegex().IsMatch(k))
      .Select(k => KeyRegex().Match(k).Groups["key"].Value);

  public async Task<Stream> OpenRead(string key) {
    string archiveKey = _archiveSource.Keys
        .FirstOrDefault(k => KeyRegex().IsMatch(k) && KeyRegex().Match(k).Groups["key"].Value == key)
        ?? throw new ArgumentException($"Key not found: {key}", nameof(key));

    return await _archiveSource.OpenRead(archiveKey);
  }

  [GeneratedRegex(@"^[^@]+@v[^/]+/(?<key>.+)$")]
  private static partial Regex KeyRegex();
}
