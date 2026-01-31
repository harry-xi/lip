using Lip.Core.JsonConverters;
using Lip.Migration;
using Scriban;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Lip.Core;

public static class PackageManifestFactory
{
    private static readonly JsonDocumentOptions _jsonDocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
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

    public static PackageManifest Create(
        JsonElement jsonElement)
    {
        return Create(jsonElement, RuntimeInformation.RuntimeIdentifier);
    }

    public static PackageManifest Create(
        JsonElement jsonElement,
        string runtimeIdentifier)
    {
        // 1. Migrate
        jsonElement = Migrator.Migrate(jsonElement);

        // 2. Apply Scriban Template Rendering
        string jsonText = jsonElement.GetRawText(); // This might not work as expected if jsonElement is a part of a larger doc.
        // Better to serialize it if we want to be safe, or if it's already a root element stringify it.
        // However, Migrator returns a new JsonElement, which is usually a root of a new generic JsonDocument or similar.
        // Let's use JsonSerializer to be safe as in original code.
        jsonText = JsonSerializer.Serialize(jsonElement, JsonSerializerOptions);

        Template template = Template.Parse(jsonText);
        // Note: The original code rendered the *PackageManifest* (deserialized object) against the *JsonElement*.
        // "RawPackageManifest.FromJsonElement(jsonElement).WithTemplateRendered();"
        // WithTemplateRendered serialized 'this' (the RawWrapper), passed it to Template.Parse, 
        // then rendered using "ToJsonElement()" (which again serialized 'this') as context.
        // Basically it used the JSON itself as the model for the template.

        string jsonTextRendered = template.Render(jsonElement); // Rendering using the jsonElement itself as model.
        JsonElement jsonElementRendered = JsonDocument.Parse(jsonTextRendered).RootElement;

        // 3. Deserialize to PackageManifest (which will now be our "Raw" representation structure but defined as PackageManifest)
        // Wait, the plan was to remove RawPackageManifest.
        // So PackageManifest itself should map to the JSON structure.

        // 4. Prune based on runtime identifier
        // We need to filter the "variants" array in the JSON *before* or *after* deserialization?
        // If PackageManifest maps 1:1 to JSON, we can deserialize first, then prune.
        // OR we can operate on JsonElement. Operating on Json is harder.
        // Let's deserialize to PackageManifest, then create a NEW PackageManifest with filtered variants.

        PackageManifest manifest = jsonElementRendered.Deserialize<PackageManifest>(JsonSerializerOptions)
            ?? throw new SchemaViolationException("", "JSON bytes deserialized to null.");

        // Validate format version and UUID (moved from original FromJsonElement)
        if (manifest.FormatVersion != PackageManifest.DefaultFormatVersion)
        {
            throw new SchemaViolationException(
                "format_version",
                $"Expected format version {PackageManifest.DefaultFormatVersion}, but got {manifest.FormatVersion}.");
        }

        if (manifest.FormatUuid != PackageManifest.DefaultFormatUuid)
        {
            throw new SchemaViolationException(
                "format_uuid",
                $"Expected format UUID '{PackageManifest.DefaultFormatUuid}', but got '{manifest.FormatUuid}'.");
        }

        manifest.Validate();

        // Prune variants
        if (manifest.Variants != null)
        {
            var prunedVariants = manifest.Variants
               .Where(v => v.Match(v.Label ?? "", runtimeIdentifier)) // Note: Match logic needs to be scrutinized.
                                                                      // The original code in GetVariant *matched* against a target Label and Platform.
                                                                      // Here we are filtering by RID. 
                                                                      // The User said: "4. 根据runtime identifier进行裁剪" (Prune based on runtime identifier).
                                                                      // This implies checking if the variant's platform is compatible with the requested RID.
                                                                      // The original Variant.Match checked:
                                                                      // if (Platform != targetPlatform) ...
                                                                      // So passing the current RID as targetPlatform should work.
                                                                      // What about Label? "v.Label" used as targetLabel?
                                                                      // If we are just pruning by RID, we probably shouldn't filter by Label yet, unless the user implies
                                                                      // "prune everything that doesn't apply to THIS machine".

               // If I pass v.Label as targetLabel, it always matches the label part (unless globbing issues).
               // So it effectively checks the platform.
               .ToList();

            // Refactoring: I should create a new manifest with pruned variants.
            manifest = manifest with { Variants = prunedVariants };
        }

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