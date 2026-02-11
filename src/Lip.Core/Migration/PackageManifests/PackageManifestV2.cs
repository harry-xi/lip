using System.Text.Json.Serialization;

namespace Lip.Core.Migration.PackageManifests;

public record PackageManifestV2
{
    [JsonPropertyName("format_version")]
    public required int FormatVersion { get; set; }

    [JsonPropertyName("tooth")]
    public required string Tooth { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("info")]
    public required PackageManifestV2Info Info { get; set; }

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

    [JsonPropertyName("platforms")]
    public List<PackageManifestV2Platform>? Platforms { get; set; }
}