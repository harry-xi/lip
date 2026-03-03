using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.Migration.PackageManifests;

public record PackageManifestV1Information {
  [JsonExtensionData]
  public Dictionary<string, JsonElement>? Data { get; set; }
}