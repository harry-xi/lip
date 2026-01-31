using Lip.Core.JsonConverters;
using Lip.Migration;
using Scriban;
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