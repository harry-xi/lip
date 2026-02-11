using System.Text.Json.Serialization;

namespace Lip.Core.Migration.PackageManifests;

public record PackageManifestV2Place
{
    [JsonPropertyName("src")]
    public required string Src { get; set; }

    [JsonPropertyName("dest")]
    public required string Dest { get; set; }
}