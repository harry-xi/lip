using System.Text.Json;
using System.Text.Json.Serialization;
using Semver;

namespace Lip.Core.Json;

public class SemVersionListJsonConverter : JsonConverter<List<SemVersion>> {
  public override List<SemVersion>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    if (reader.TokenType == JsonTokenType.Null) {
      return null;
    }

    if (reader.TokenType != JsonTokenType.StartArray) {
      throw new JsonException($"Expected StartArray token, got {reader.TokenType}.");
    }

    List<SemVersion> versions = [];

    while (reader.Read()) {
      if (reader.TokenType == JsonTokenType.EndArray) {
        return versions;
      }

      if (reader.TokenType == JsonTokenType.String) {
        string? value = reader.GetString() ?? throw new JsonException("SemVersion value cannot be null.");
        versions.Add(SemVersion.Parse(value, SemVersionStyles.Any));
        continue;
      }

      throw new JsonException($"Expected String token, got {reader.TokenType}.");
    }

    throw new JsonException("Unexpected end of JSON while reading SemVersion list.");
  }

  public override void Write(Utf8JsonWriter writer, List<SemVersion> value, JsonSerializerOptions options) {
    writer.WriteStartArray();

    foreach (SemVersion version in value) {
      writer.WriteStringValue(version.ToString());
    }

    writer.WriteEndArray();
  }
}