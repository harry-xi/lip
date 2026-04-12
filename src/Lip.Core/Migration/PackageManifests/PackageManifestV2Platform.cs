using System.Text.Json.Serialization;

namespace Lip.Core.Migration.PackageManifests;

public record PackageManifestV2Platform {
  [JsonPropertyName("goarch")]
  public string? GOARCH { get; set; }

  [JsonPropertyName("goos")]
  public required string GOOS { get; set; }

  [JsonPropertyName("asset_url")]
  public string? AssetUrl { get; set; }

  [JsonPropertyName("commands")]
  public PackageManifestV2Commands? Commands { get; set; }

  [JsonPropertyName("dependencies")]
  public Dictionary<string, string>? Dependencies { get; set; }

  [JsonPropertyName("prerequisites")]
  public Dictionary<string, string>? Prerequisites { get; set; }

  [JsonPropertyName("files")]
  public PackageManifestV2Files? Files { get; set; }
}
