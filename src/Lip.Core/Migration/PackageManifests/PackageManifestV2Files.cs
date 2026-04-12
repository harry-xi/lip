using System.Text.Json.Serialization;

namespace Lip.Core.Migration.PackageManifests;

public record PackageManifestV2Files {
  [JsonPropertyName("place")]
  public List<PackageManifestV2Place>? Place { get; set; }

  [JsonPropertyName("preserve")]
  public List<string>? Preserve { get; set; }

  [JsonPropertyName("remove")]
  public List<string>? Remove { get; set; }
}
