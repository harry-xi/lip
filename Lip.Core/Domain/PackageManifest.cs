using DotNet.Globbing;
using Flurl;
using Lip.Core.JsonConverters;
using Semver;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Lip.Core;

/// <summary>
/// Represents the package manifest.
/// </summary>
public record PackageManifest
{
    public const int DefaultFormatVersion = 3;
    public const string DefaultFormatUuid = "289f771f-2c9a-4d73-9f3f-8492495a924d";

    [JsonPropertyName("format_version")]
    public required int FormatVersion { get; init; }

    [JsonPropertyName("format_uuid")]
    public required string FormatUuid { get; init; }

    [JsonPropertyName("tooth")]
    public required string ToothPath { get; init; }

    [JsonPropertyName("version")]
    [JsonConverter(typeof(SemVersionConverter))]
    public required SemVersion Version { get; init; }

    [JsonPropertyName("info")]
    public InfoType Info { get; init; } = new();

    [JsonPropertyName("variants")]
    public List<Variant> Variants { get; init; } = [];

    public record Asset
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum TypeEnum
        {
            [JsonStringEnumMemberName("self")]
            Self,
            [JsonStringEnumMemberName("tar")]
            Tar,
            [JsonStringEnumMemberName("tgz")]
            Tgz,
            [JsonStringEnumMemberName("uncompressed")]
            Uncompressed,
            [JsonStringEnumMemberName("zip")]
            Zip,
        }

        [JsonPropertyName("type")]
        public required TypeEnum Type { get; init; }

        [JsonPropertyName("urls")]
        public List<Url> Urls { get; init; } = [];

        [JsonPropertyName("placements")]
        public List<Placement> Placements { get; init; } = [];
    }

    public record InfoType
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("description")]
        public string Description { get; init; } = "";

        [JsonPropertyName("tags")]
        public List<string> Tags { get; init; } = [];

        [JsonPropertyName("avatar_url")]
        [JsonConverter(typeof(UrlConverter))]
        public Url? AvatarUrl { get; init; }
    }

    public record Placement
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum TypeEnum
        {
            [JsonStringEnumMemberName("file")]
            File,
            [JsonStringEnumMemberName("dir")]
            Dir,
        }

        [JsonPropertyName("type")]
        public required TypeEnum Type { get; init; }

        [JsonPropertyName("src")]
        public required string Src { get; init; }

        [JsonPropertyName("dest")]
        public required string Dest { get; init; }
    }

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
        public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

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

    public record Variant
    {
        [JsonPropertyName("label")]
        public string Label { get; init; } = "";

        [JsonPropertyName("platform")]
        public string Platform { get; init; } = "";

        [JsonPropertyName("dependencies")]
        public Dictionary<PackageIdentifier, SemVersionRange> Dependencies { get; init; } = [];

        [JsonPropertyName("assets")]
        public List<Asset> Assets { get; init; } = [];

        [JsonPropertyName("preserve_files")]
        public List<string> PreserveFiles { get; init; } = [];

        [JsonPropertyName("remove_files")]
        public List<string> RemoveFiles { get; init; } = [];

        [JsonPropertyName("scripts")]
        public ScriptsType Scripts { get; init; } = new();

        public bool Match(string targetLabel, string targetPlatform)
        {
            string label = Label ?? "";
            string platform = Platform ?? "";

            // Check if the variant label matches the specified label.
            if (label != targetLabel)
            {
                if (label == string.Empty)
                {
                    return false;
                }

                if (!Glob.Parse(label).IsMatch(targetLabel))
                {
                    return false;
                }
            }

            // Check if the platform matches the specified platform.
            if (platform != targetPlatform)
            {
                if (platform == string.Empty)
                {
                    return false;
                }

                if (!Glob.Parse(platform).IsMatch(targetPlatform))
                {
                    return false;
                }
            }

            return true;
        }
    }

    // Kept for backward compatibility or business logic helper, but logic is simplified
    public Variant? GetVariant(string targetLabel, string targetPlatform)
    {
        if (Variants == null) return null;

        // Find the variant that matches the specified label and platform.
        List<Variant> matchedVariants = Variants.Where(variant => variant.Match(targetLabel, targetPlatform)).ToList();

        // There must be at least one variant fully matched (Exact match check from original code)
        // Original code: variant.Label == targetLabel && variant.Platform == targetPlatform
        // Since we are now using nullables, we handle that.
        if (!matchedVariants.Any(
            variant => (variant.Label ?? "") == targetLabel
                       && (variant.Platform ?? "") == targetPlatform))
        {
            return null;
        }

        // Merge all matched variants into a single variant.
        Variant mergedVariant = new()
        {
            Label = targetLabel,
            Platform = targetPlatform,
            Dependencies = matchedVariants
                .SelectMany(variant => variant.Dependencies)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            Assets = matchedVariants.SelectMany(variant => variant.Assets).ToList(),
            PreserveFiles = matchedVariants.SelectMany(variant => variant.PreserveFiles).ToList(),
            RemoveFiles = matchedVariants.SelectMany(variant => variant.RemoveFiles).ToList(),
            Scripts = new ScriptsType
            {
                PreInstall = matchedVariants.LastOrDefault(v => v.Scripts.PreInstall.Count > 0)?.Scripts.PreInstall ?? [],
                Install = matchedVariants.LastOrDefault(v => v.Scripts.Install.Count > 0)?.Scripts.Install ?? [],
                PostInstall = matchedVariants.LastOrDefault(v => v.Scripts.PostInstall.Count > 0)?.Scripts.PostInstall ?? [],
                PrePack = matchedVariants.LastOrDefault(v => v.Scripts.PrePack.Count > 0)?.Scripts.PrePack ?? [],
                PostPack = matchedVariants.LastOrDefault(v => v.Scripts.PostPack.Count > 0)?.Scripts.PostPack ?? [],
                PreUninstall = matchedVariants.LastOrDefault(v => v.Scripts.PreUninstall.Count > 0)?.Scripts.PreUninstall ?? [],
                Uninstall = matchedVariants.LastOrDefault(v => v.Scripts.Uninstall.Count > 0)?.Scripts.Uninstall ?? [],
                PostUninstall = matchedVariants.LastOrDefault(v => v.Scripts.PostUninstall.Count > 0)?.Scripts.PostUninstall ?? [],
                AdditionalProperties = matchedVariants
                     .Where(v => v.Scripts.AdditionalProperties != null)
                     .SelectMany(variant => variant.Scripts.AdditionalProperties!)
                     .GroupBy(kvp => kvp.Key)
                     .ToDictionary(kvp => kvp.Key, kvp => kvp.Last().Value)
            }
        };

        return mergedVariant;
    }

    // Validation methods - kept as statics
    public static bool IsValidTag(string tag)
    {
        return new Regex("^[a-z0-9-]+(:[a-z0-9-]+)?$").IsMatch(tag);
    }

    public static bool IsValidScriptName(string scriptName)
    {
        return new Regex("^[a-z0-9]+(_[a-z0-9]+)*$").IsMatch(scriptName);
    }

    public static bool IsValidPlacementDest(string path)
    {
        if (Path.IsPathFullyQualified(path) || Path.IsPathRooted(path))
        {
            return false;
        }

        if (path.Contains(".."))
        {
            return false;
        }

        return true;
    }

    public void Validate()
    {
        foreach (var tag in Info.Tags)
        {
            if (!IsValidTag(tag))
            {
                throw new SchemaViolationException("info.tags[]", $"Invalid tag '{tag}'.");
            }
        }

        foreach (var variant in Variants)
        {
            foreach (var asset in variant.Assets)
            {
                foreach (var placement in asset.Placements)
                {
                    if (!IsValidPlacementDest(placement.Dest))
                    {
                        throw new SchemaViolationException("variants[].assets[].placements[].dest", $"Invalid destination path '{placement.Dest}'.");
                    }
                }
            }

            foreach (var preserveFile in variant.PreserveFiles)
            {
                if (!IsValidPlacementDest(preserveFile))
                {
                    throw new SchemaViolationException("variants[].preserve_files[]", $"Invalid preserve file path '{preserveFile}'.");
                }
            }

            foreach (var removeFile in variant.RemoveFiles)
            {
                if (!IsValidPlacementDest(removeFile))
                {
                    throw new SchemaViolationException("variants[].remove_file[]", $"Invalid remove file path '{removeFile}'.");
                }
            }

            if (variant.Scripts.AdditionalProperties != null)
            {
                foreach (var key in variant.Scripts.AdditionalProperties.Keys)
                {
                    if (!IsValidScriptName(key))
                    {
                        throw new SchemaViolationException($"variants[].assets[].scripts.'{key}'", $"Invalid script name '{key}'.");
                    }
                }
            }
        }
    }
}