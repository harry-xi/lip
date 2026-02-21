using Lip.Core.Json;
using Semver;
using System.Text.Json.Serialization;

namespace Lip.Core.Entities;

public record PackageIndexPackage
{
    [JsonPropertyName("info")]
    public required PackageManifestInfo Info { get; init; }

    [JsonPropertyName("updated_at")]
    public required DateTime UpdatedAt { get; init; }

    [JsonPropertyName("stars")]
    public required int Stars { get; init; }

    [JsonConverter(typeof(SemVersionKeyStringListDictJsonConverter))]
    [JsonPropertyName("versions")]
    public required Dictionary<SemVersion, List<string>> Versions { get; init; }
}