using System.Text.Json;
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

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalProperties
        {
            get;
            init
            {
                if (value != null)
                {
                    foreach (var key in value.Keys)
                    {
                        if (!IsValidScriptName(key))
                            throw new SchemaViolationException($"scripts.'{key}'", $"Invalid script name '{key}'.");
                    }
                }
                field = value;
            }
        }

        [JsonIgnore]
        public Dictionary<string, List<string>> AdditionalScripts
        {
            get
            {
                if (AdditionalProperties == null) return [];
                return AdditionalProperties
                   .Where(kvp => kvp.Value.ValueKind == JsonValueKind.Array && kvp.Value.EnumerateArray().All(e => e.ValueKind == JsonValueKind.String))
                   .ToDictionary(
                       kvp => kvp.Key,
                       kvp => kvp.Value.Deserialize<List<string>>() ?? []
                   );
            }
        }
    }
}