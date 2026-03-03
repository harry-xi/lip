using System.Text.Json.Serialization;

namespace Lip.Core.Migration.PackageManifests;

public record PackageManifestV1Placement {
  [JsonPropertyName("source")]
  public required string Source { get; set; }

  [JsonPropertyName("destination")]
  public required string Destination { get; set; }

  [JsonPropertyName("GOOS")]
  public string? GOOS { get; set; }

  [JsonPropertyName("GOARCH")]
  public string? GOARCH { get; set; }
}