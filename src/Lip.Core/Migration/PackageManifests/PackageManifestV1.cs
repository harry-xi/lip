using System.Text.Json.Serialization;

namespace Lip.Core.Migration.PackageManifests;

public record PackageManifestV1 {
  [JsonPropertyName("format_version")]
  public required int FormatVersion { get; set; }

  [JsonPropertyName("tooth")]
  public required string Tooth { get; set; }

  [JsonPropertyName("version")]
  public required string Version { get; set; }

  [JsonPropertyName("dependencies")]
  public Dictionary<string, List<List<string>>>? Dependencies { get; set; }

  [JsonPropertyName("information")]
  public PackageManifestV1Information? Information { get; set; }

  [JsonPropertyName("placement")]
  public List<PackageManifestV1Placement>? Placement { get; set; }

  [JsonPropertyName("possession")]
  public List<string>? Possession { get; set; }

  [JsonPropertyName("commands")]
  public List<PackageManifestV1Command>? Commands { get; set; }
}
