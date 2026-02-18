using Lip.Core.Json;
using Semver;
using System.Text.Json.Serialization;

namespace Lip.Core.Entities;

public record PackageIndexPackage
{
    [JsonPropertyName("tooth")]
    public required string Path { get; init; }

    [JsonPropertyName("info")]
    public required PackageManifestInfo Info { get; init; }

    [JsonConverter(typeof(SemVersionListJsonConverter))]
    [JsonPropertyName("versions")]
    public required List<SemVersion> Versions { get; init; }
}