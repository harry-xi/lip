using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Globbing;

namespace Lip.Core.Json;

public class GlobListJsonConverter : JsonConverter<List<Glob>> {
  public override List<Glob>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    if (reader.TokenType != JsonTokenType.StartArray) {
      throw new JsonException("Expected start of array");
    }

    var list = new List<Glob>();

    while (reader.Read()) {
      if (reader.TokenType == JsonTokenType.EndArray) {
        return list;
      }

      if (reader.TokenType == JsonTokenType.String) {
        string? s = reader.GetString();
        if (!string.IsNullOrEmpty(s)) {
          list.Add(Glob.Parse(s));
        }
      } else {
        throw new JsonException($"Expected string but got {reader.TokenType}");
      }
    }

    throw new JsonException("Expected end of array");
  }

  public override void Write(Utf8JsonWriter writer, List<Glob> value, JsonSerializerOptions options) {
    writer.WriteStartArray();
    foreach (var glob in value) {
      writer.WriteStringValue(glob.ToString());
    }
    writer.WriteEndArray();
  }
}
