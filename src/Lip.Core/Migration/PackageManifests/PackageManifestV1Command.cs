using System.Text.Json.Serialization;

namespace Lip.Core.Migration.PackageManifests;

public record PackageManifestV1Command {
  [JsonPropertyName("type")]
  public required string Type { get; set; }

  [JsonPropertyName("commands")]
  public required List<string> Commands { get; set; }

  [JsonPropertyName("GOOS")]
  public string? GOOS { get; set; }

  [JsonPropertyName("GOARCH")]
  public string? GOARCH { get; set; }
}