using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip;

public record PackageManifest
{
    [JsonPropertyName("format_version")]
    public required string FormatVersion;

    [JsonPropertyName("format_uuid")]
    public required string FormatUuid;

    [JsonPropertyName("tooth")]
    public required string Tooth;

    [JsonPropertyName("version")]
    public required string Version;

    [JsonPropertyName("info")]
    public InfoType? Info;

    public record InfoType
    {
        [JsonPropertyName("name")]
        public string? Name;

        [JsonPropertyName("description")]
        public string? Description;
    }
}
