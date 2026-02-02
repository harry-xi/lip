using System.Text.Json.Serialization;

namespace Lip.Core;

public partial record PackageManifest
{
    public record ScriptsType
    {
        [JsonPropertyName("pre_install")]
        public List<string> PreInstall { get; init; } = [];

        [JsonPropertyName("install")]
        public List<string> Install { get; init; } = [];

        [JsonPropertyName("post_install")]
        public List<string> PostInstall { get; init; } = [];

        [JsonPropertyName("pre_pack")]
        public List<string> PrePack { get; init; } = [];

        [JsonPropertyName("post_pack")]
        public List<string> PostPack { get; init; } = [];

        [JsonPropertyName("pre_uninstall")]
        public List<string> PreUninstall { get; init; } = [];

        [JsonPropertyName("uninstall")]
        public List<string> Uninstall { get; init; } = [];

        [JsonPropertyName("post_uninstall")]
        public List<string> PostUninstall { get; init; } = [];


    }
}