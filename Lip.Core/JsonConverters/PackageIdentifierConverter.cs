using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.JsonConverters;

public class PackageIdentifierConverter : JsonConverter<PackageIdentifier>
{
    public override PackageIdentifier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (value == null) return null;
            return PackageIdentifier.Parse(value);
        }
        throw new JsonException($"Unexpected token {reader.TokenType} when parsing PackageIdentifier.");
    }

    public override void Write(Utf8JsonWriter writer, PackageIdentifier value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    public override PackageIdentifier ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString() ?? throw new JsonException("Null property name for PackageIdentifier");
        return PackageIdentifier.Parse(value);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, PackageIdentifier value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }
}