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
public partial record PackageManifest
{
    public const int DefaultFormatVersion = 3;
    public const string DefaultFormatUuid = "289f771f-2c9a-4d73-9f3f-8492495a924d";

    [JsonPropertyName("format_version")]
    public required int FormatVersion
    {
        get => field;
        init => field = value == DefaultFormatVersion
            ? value
            : throw new SchemaViolationException("format_version", $"Expected {DefaultFormatVersion}, got {value}.");
    }

    [JsonPropertyName("format_uuid")]
    public required string FormatUuid
    {
        get => field;
        init => field = value == DefaultFormatUuid
            ? value
            : throw new SchemaViolationException("format_uuid", $"Expected '{DefaultFormatUuid}', got '{value}'.");
    }

    [JsonPropertyName("tooth")]
    public required string ToothPath
    {
        get => field;
        init => field = PackageIdentifier.IsValidToothPath(value)
            ? value
            : throw new SchemaViolationException("tooth", $"Invalid tooth path '{value}'.");
    }

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
        public List<string> Tags
        {
            get => field;
            init
            {
                foreach (var tag in value)
                {
                    if (!IsValidTag(tag))
                        throw new SchemaViolationException("info.tags[]", $"Invalid tag '{tag}'.");
                }
                field = value;
            }
        } = [];

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
        public required string Dest
        {
            get => field;
            init => field = IsValidPlacementDest(value)
                ? value
                : throw new SchemaViolationException("placements[].dest", $"Invalid destination path '{value}'.");
        }
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
        public Dictionary<string, JsonElement>? AdditionalProperties
        {
            get => field;
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
        public List<string> PreserveFiles
        {
            get => field;
            init
            {
                foreach (var file in value)
                {
                    if (!IsValidPlacementDest(file))
                        throw new SchemaViolationException("variants[].preserve_files[]", $"Invalid preserve file path '{file}'.");
                }
                field = value;
            }
        } = [];

        [JsonPropertyName("remove_files")]
        public List<string> RemoveFiles
        {
            get => field;
            init
            {
                foreach (var file in value)
                {
                    if (!IsValidPlacementDest(file))
                        throw new SchemaViolationException("variants[].remove_files[]", $"Invalid remove file path '{file}'.");
                }
                field = value;
            }
        } = [];

        [JsonPropertyName("scripts")]
        public ScriptsType Scripts { get; init; } = new();

        public bool Match(string targetLabel, string targetPlatform)
        {
            string label = Label ?? "";
            string platform = Platform ?? "";

            if (label != targetLabel)
            {
                if (label == string.Empty)
                    return false;
                if (!Glob.Parse(label).IsMatch(targetLabel))
                    return false;
            }

            if (platform != targetPlatform)
            {
                if (platform == string.Empty)
                    return false;
                if (!Glob.Parse(platform).IsMatch(targetPlatform))
                    return false;
            }

            return true;
        }
    }

    public Variant? GetVariant(string targetLabel, string targetPlatform)
    {
        if (Variants == null) return null;

        List<Variant> matchedVariants = Variants.Where(variant => variant.Match(targetLabel, targetPlatform)).ToList();

        if (!matchedVariants.Any(
            variant => (variant.Label ?? "") == targetLabel
                       && (variant.Platform ?? "") == targetPlatform))
        {
            return null;
        }

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

    [GeneratedRegex("^[a-z0-9-]+(:[a-z0-9-]+)?$")]
    private static partial Regex TagRegex();

    public static bool IsValidTag(string tag) => TagRegex().IsMatch(tag);

    [GeneratedRegex("^[a-z0-9]+(_[a-z0-9]+)*$")]
    private static partial Regex ScriptNameRegex();

    public static bool IsValidScriptName(string scriptName) => ScriptNameRegex().IsMatch(scriptName);

    public static bool IsValidPlacementDest(string path)
    {
        if (Path.IsPathFullyQualified(path) || Path.IsPathRooted(path))
            return false;
        if (path.Contains(".."))
            return false;
        return true;
    }
}