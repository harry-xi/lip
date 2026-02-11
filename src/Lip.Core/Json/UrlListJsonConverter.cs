using Flurl;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.Json;

public class UrlListJsonConverter : JsonConverter<List<Url>>
{
    public override List<Url>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array");
        }

        var list = new List<Url>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return list;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                string? s = reader.GetString();
                if (!string.IsNullOrEmpty(s))
                {
                    list.Add(Url.Parse(s));
                }
            }
            else
            {
                throw new JsonException($"Expected string but got {reader.TokenType}");
            }
        }

        throw new JsonException("Expected end of array");
    }

    public override void Write(Utf8JsonWriter writer, List<Url> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var url in value)
        {
            writer.WriteStringValue(url.ToString());
        }
        writer.WriteEndArray();
    }
}