using Flurl;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.JsonConverters;

public class UrlConverter : JsonConverter<Url>
{
    public override Url? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (value == null) return null;
            return Url.Parse(value);
        }
        throw new JsonException($"Unexpected token {reader.TokenType} when parsing Url.");
    }

    public override void Write(Utf8JsonWriter writer, Url value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}