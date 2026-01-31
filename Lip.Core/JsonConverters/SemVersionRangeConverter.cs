using Semver;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.JsonConverters;

public class SemVersionRangeConverter : JsonConverter<SemVersionRange>
{
    public override SemVersionRange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (value == null) return null;
            return SemVersionRange.Parse(value);
        }
        throw new JsonException($"Unexpected token {reader.TokenType} when parsing SemVersionRange.");
    }

    public override void Write(Utf8JsonWriter writer, SemVersionRange value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}