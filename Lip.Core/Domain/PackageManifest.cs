using Lip.Core.JsonConverters;
using Lip.Migration;
using Scriban;
using Semver;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Lip.Core;

public partial record PackageManifest
{
    private static readonly JsonDocumentOptions _jsonDocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IndentSize = 4,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        Converters =
        {
            new SemVersionConverter(),
            new UrlConverter(),
            new PackageIdentifierConverter(),
            new SemVersionRangeConverter(),
        },
    };

    private const int DefaultFormatVersion = 3;
    private const string DefaultFormatUuid = "289f771f-2c9a-4d73-9f3f-8492495a924d";

    [JsonConstructor]
    public PackageManifest(int FormatVersion, string? FormatUuid)
    {
        this.FormatVersion = FormatVersion;
        this.FormatUuid = FormatUuid ?? "";
    }

    public PackageManifest()
    {
    }

    [JsonPropertyName("format_version")]
    public int FormatVersion
    {
        get;
        init => field = value == DefaultFormatVersion
            ? value
            : throw new SchemaViolationException("format_version", $"Expected {DefaultFormatVersion}, got {value}.");
    } = DefaultFormatVersion;

    [JsonPropertyName("format_uuid")]
    public string FormatUuid
    {
        get;
        init => field = value == DefaultFormatUuid
            ? value
            : throw new SchemaViolationException("format_uuid", $"Expected '{DefaultFormatUuid}', got '{value}'.");
    } = DefaultFormatUuid;

    [JsonPropertyName("tooth")]
    public required string ToothPath
    {
        get;
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

    public static PackageManifest Create(JsonElement jsonElement)
    {
        // 1. Migrate
        jsonElement = Migrator.Migrate(jsonElement);

        // 2. Apply Scriban Template Rendering
        string jsonText = JsonSerializer.Serialize(jsonElement, JsonSerializerOptions);

        Template template = Template.Parse(jsonText);
        string jsonTextRendered = template.Render(jsonElement);
        JsonElement jsonElementRendered = JsonDocument.Parse(jsonTextRendered).RootElement;

        // 3. Deserialize to PackageManifest (validation happens in property setters)
        PackageManifest manifest = jsonElementRendered.Deserialize<PackageManifest>(JsonSerializerOptions)
            ?? throw new SchemaViolationException("", "JSON bytes deserialized to null.");

        return manifest;
    }

    public static async Task<PackageManifest> FromStream(Stream stream)
    {
        JsonElement jsonElement = (await JsonDocument.ParseAsync(
           stream,
           _jsonDocumentOptions)).RootElement;

        return Create(jsonElement);
    }

    public static async Task WriteToStreamAsync(PackageManifest manifest, Stream stream)
    {
        await JsonSerializer.SerializeAsync(stream, manifest, JsonSerializerOptions);
    }
}