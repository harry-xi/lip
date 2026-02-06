using System.Text.Json.Serialization;

namespace Lip.Core.Entities;

public record PackageManifestScripts
{
    [JsonPropertyName("pre_install")]
    public List<string> PreInstall { get; init; } = [];

    [JsonPropertyName("post_install")]
    public List<string> PostInstall { get; init; } = [];

    [JsonPropertyName("pre_uninstall")]
    public List<string> PreUninstall { get; init; } = [];

    [JsonPropertyName("post_uninstall")]
    public List<string> PostUninstall { get; init; } = [];
}