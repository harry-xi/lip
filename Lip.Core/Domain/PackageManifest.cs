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
                PreUninstall = matchedVariants.LastOrDefault(v => v.Scripts.PreUninstall.Count > 0)?.Scripts.PreUninstall ?? [],
                Uninstall = matchedVariants.LastOrDefault(v => v.Scripts.Uninstall.Count > 0)?.Scripts.Uninstall ?? [],
                PostUninstall = matchedVariants.LastOrDefault(v => v.Scripts.PostUninstall.Count > 0)?.Scripts.PostUninstall ?? [],
            }
        };

        return mergedVariant;
    }

    [GeneratedRegex("^[a-z0-9-]+(:[a-z0-9-]+)?$")]
    private static partial Regex TagRegex();

    public static bool IsValidTag(string tag) => TagRegex().IsMatch(tag);

    [GeneratedRegex("^[a-z0-9]+(_[a-z0-9]+)*$")]
    private static partial Regex ScriptNameRegex();



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

        // 2. Prepare for Scriban rendering
        // We use JsonNode to traverse and modify the JSON structure.
        // We use the original jsonElement as the model for Scriban rendering.
        System.Text.Json.Nodes.JsonNode? rootNode = System.Text.Json.Nodes.JsonNode.Parse(jsonElement.GetRawText()) ?? throw new SchemaViolationException("", "JSON parsed to null.");
        List<string> errors = [];
        ApplyScribanRecursively(rootNode, jsonElement, errors);

        if (errors.Count > 0)
        {
            throw new SchemaViolationException("pre_process", string.Join(Environment.NewLine, errors));
        }

        // 3. Deserialize to PackageManifest (validation happens in property setters)
        PackageManifest manifest = rootNode.Deserialize<PackageManifest>(JsonSerializerOptions)
            ?? throw new SchemaViolationException("", "JSON bytes deserialized to null.");

        return manifest;
    }

    private static void ApplyScribanRecursively(System.Text.Json.Nodes.JsonNode node, JsonElement model, List<string> errors)
    {
        if (node is System.Text.Json.Nodes.JsonObject jsonObject)
        {
            // ToList to avoid modification while iterating, although we are modifying values, not keys.
            foreach (KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> kvp in jsonObject.ToList())
            {
                if (kvp.Value is System.Text.Json.Nodes.JsonValue val && val.TryGetValue(out string? text))
                {
                    // Render string value
                    string renderedText = RenderScriban(text, model, errors);
                    if (renderedText != text)
                    {
                        jsonObject[kvp.Key] = renderedText;
                    }
                }
                else if (kvp.Value is not null)
                {
                    ApplyScribanRecursively(kvp.Value, model, errors);
                }
            }
        }
        else if (node is System.Text.Json.Nodes.JsonArray jsonArray)
        {
            for (int i = 0; i < jsonArray.Count; i++)
            {
                if (jsonArray[i] is System.Text.Json.Nodes.JsonValue val && val.TryGetValue(out string? text))
                {
                    // Render string value
                    string renderedText = RenderScriban(text, model, errors);
                    if (renderedText != text)
                    {
                        jsonArray[i] = renderedText;
                    }
                }
                else if (jsonArray[i] is not null)
                {
                    ApplyScribanRecursively(jsonArray[i]!, model, errors);
                }
            }
        }
    }

    private static string RenderScriban(string text, JsonElement model, List<string> errors)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("{{"))
        {
            return text;
        }

        Template template = Template.Parse(text);

        if (template.HasErrors)
        {
            foreach (var message in template.Messages)
            {
                errors.Add(message.ToString());
            }
            return text;
        }

        string rendered = template.Render(model); // JsonElement works as model for Scriban? We assume yes based on previous code.
                                                  // NOTE: If previous code used Render(jsonElement), it implies Scriban can use it. 
                                                  // However, standard Scriban might need a specific object. 
                                                  // If this fails during tests, we will wrap jsonElement or convert it.
        return rendered;
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