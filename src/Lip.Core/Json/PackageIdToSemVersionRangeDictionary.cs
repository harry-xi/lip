using System.Text.Json;
using System.Text.Json.Serialization;
using Lip.Core.Entities;
using Semver;

namespace Lip.Core.Json;

public class PackageIdToSemVersionRangeDictionary : JsonConverter<Dictionary<PackageId, SemVersionRange>> {
  public override Dictionary<PackageId, SemVersionRange>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    if (reader.TokenType != JsonTokenType.StartObject) {
      throw new JsonException();
    }

    Dictionary<PackageId, SemVersionRange> dictionary = [];

    while (reader.Read()) {
      if (reader.TokenType == JsonTokenType.EndObject) {
        return dictionary;
      }

      if (reader.TokenType != JsonTokenType.PropertyName) {
        throw new JsonException();
      }

      string? propertyName = reader.GetString() ?? throw new JsonException();
      PackageId key = PackageId.Parse(propertyName);

      reader.Read();
      if (reader.TokenType != JsonTokenType.String) {
        throw new JsonException();
      }

      string? value = reader.GetString() ?? throw new JsonException();
      SemVersionRange range = SemVersionRange.Parse(value);
      dictionary.Add(key, range);
    }

    throw new JsonException();
  }

  public override void Write(Utf8JsonWriter writer, Dictionary<PackageId, SemVersionRange> value, JsonSerializerOptions options) {
    writer.WriteStartObject();

    foreach (KeyValuePair<PackageId, SemVersionRange> kvp in value) {
      writer.WritePropertyName(kvp.Key.ToString());
      writer.WriteStringValue(kvp.Value.ToString());
    }

    writer.WriteEndObject();
  }
}