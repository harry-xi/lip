using Semver;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.Json;

public class SemVersionKeyStringListDictJsonConverter : JsonConverter<Dictionary<SemVersion, List<string>>>
{
    public override Dictionary<SemVersion, List<string>>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        Dictionary<SemVersion, List<string>> dictionary = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string? propertyName = reader.GetString() ?? throw new JsonException();
            SemVersion key = SemVersion.Parse(propertyName, SemVersionStyles.Any);

            List<string>? value = JsonSerializer.Deserialize<List<string>>(ref reader, options);
            if (value is null)
            {
                throw new JsonException();
            }

            dictionary.Add(key, value);
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<SemVersion, List<string>> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<SemVersion, List<string>> kvp in value)
        {
            writer.WritePropertyName(kvp.Key.ToString());
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}