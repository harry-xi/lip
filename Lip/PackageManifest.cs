using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip;

public record PackageManifest
{
    public record AssetType
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("urls")]
        public List<string>? Urls { get; init; }

        [JsonPropertyName("place")]
        public List<PlaceType>? Place { get; init; }

        [JsonPropertyName("preserve")]
        public List<string>? Preserve { get; init; }

        [JsonPropertyName("remove")]
        public List<string>? Remove { get; init; }
    }

    public record InfoType
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("author")]
        public string? Author { get; init; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; init; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; init; }
    }

    public record PlaceType
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("src")]
        public required string Src { get; init; }

        [JsonPropertyName("dest")]
        public required string Dest { get; init; }
    }

    public record ScriptsType
    {
        [JsonPropertyName("pre_install")]
        public List<string>? PreInstall { get; init; }

        [JsonPropertyName("install")]
        public List<string>? Install { get; init; }

        [JsonPropertyName("post_install")]
        public List<string>? PostInstall { get; init; }

        [JsonPropertyName("pre_pack")]
        public List<string>? PrePack { get; init; }

        [JsonPropertyName("post_pack")]
        public List<string>? PostPack { get; init; }

        [JsonPropertyName("pre_uninstall")]
        public List<string>? PreUninstall { get; init; }

        [JsonPropertyName("uninstall")]
        public List<string>? Uninstall { get; init; }

        [JsonPropertyName("post_uninstall")]
        public List<string>? PostUninstall { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

        [JsonIgnore]
        public Dictionary<string, List<string>> AdditionalScripts
        {
            get
            {
                var additionalScripts = new Dictionary<string, List<string>>();
                if (AdditionalProperties is not null)
                {
                    foreach (KeyValuePair<string, JsonElement> kvp in AdditionalProperties)
                    {
                        List<string>? scripts = JsonSerializer.Deserialize<List<string>>(kvp.Value.GetRawText());
                        additionalScripts[kvp.Key] = scripts ?? [];
                    }
                }
                return additionalScripts;
            }
        }
    }

    public record VariantType
    {
        [JsonPropertyName("platform")]
        public required string Platform { get; init; }

        [JsonPropertyName("dependencies")]
        public Dictionary<string, string>? Dependencies { get; init; }

        [JsonPropertyName("prerequisites")]
        public Dictionary<string, string>? Prerequisites { get; init; }

        [JsonPropertyName("assets")]
        public List<AssetType>? Assets { get; init; }

        [JsonPropertyName("scripts")]
        public ScriptsType? Scripts { get; init; }
    }

    [JsonPropertyName("format_version")]
    public required int FormatVersion
    {
        get => 3;
        init => _ = value == 3 ? 0
            : throw new ArgumentException("FormatVersion must be 3", nameof(value));
    }

    [JsonPropertyName("format_uuid")]
    public required string FormatUuid
    {
        get => "289f771f-2c9a-4d73-9f3f-8492495a924d";
        init => _ = value == "289f771f-2c9a-4d73-9f3f-8492495a924d" ? 0
            : throw new ArgumentException("FormatUuid must be 114514", nameof(value));
    }

    [JsonPropertyName("tooth")]
    public required string Tooth { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("info")]
    public InfoType? Info { get; init; }

    [JsonPropertyName("variants")]
    public VariantType[]? Variants { get; init; }

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static PackageManifest? FromBytes(byte[] bytes)
    {
        PackageManifest? manifest = JsonSerializer.Deserialize<PackageManifest>(
            bytes,
            s_jsonSerializerOptions
        );

        // Validate additional properties of scripts.
        if (manifest?.Variants is not null)
        {
            foreach (VariantType variant in manifest.Variants)
            {
                if (variant.Scripts?.AdditionalProperties is not null)
                {
                    foreach (KeyValuePair<string, JsonElement> kvp in variant.Scripts.AdditionalProperties)
                    {
                        if (kvp.Value.ValueKind != JsonValueKind.Array)
                        {
                            throw new JsonException("Self-defined scripts must be arrays of strings.");
                        }

                        foreach (JsonElement element in kvp.Value.EnumerateArray())
                        {
                            if (element.ValueKind != JsonValueKind.String)
                            {
                                throw new JsonException("Self-defined scripts must be arrays of strings.");
                            }
                        }
                    }
                }
            }
        }

        return manifest;
    }
}
