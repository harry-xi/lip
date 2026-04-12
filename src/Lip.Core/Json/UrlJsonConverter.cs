using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;

namespace Lip.Core.Json;

public class UrlJsonConverter : JsonConverter<Url> {
  public override Url? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    string? s = reader.GetString();
    return s is null ? null : Url.Parse(s);
  }

  public override void Write(Utf8JsonWriter writer, Url value, JsonSerializerOptions options) {
    writer.WriteStringValue(value.ToString());
  }
}
