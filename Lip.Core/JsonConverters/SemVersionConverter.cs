using Semver;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.JsonConverters;

public class SemVersionConverter : JsonConverter<SemVersion>
{
    public override SemVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (value == null) return null;
            return SemVersion.Parse(value);
        }
        throw new JsonException($"Unexpected token {reader.TokenType} when parsing SemVersion.");
    }

    public override void Write(Utf8JsonWriter writer, SemVersion value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}