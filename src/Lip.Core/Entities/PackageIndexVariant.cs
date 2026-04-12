using System.Text.Json.Serialization;
using Lip.Core.Json;
using Semver;


namespace Lip.Core.Entities;

public record PackageIndexVariant {
  [JsonConverter(typeof(SemVersionListJsonConverter))]
  [JsonPropertyName("versions")]
  public required List<SemVersion> Versions { get; init; }
}
