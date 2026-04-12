using System.Text.Json;
using System.Text.Json.Serialization;
using Semver;

namespace Lip.Core.Json;

public class SemVersionJsonConverter : JsonConverter<SemVersion> {
  public override SemVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    string? s = reader.GetString();
    return s is null ? null : SemVersion.Parse(s, SemVersionStyles.Any);
  }

  public override void Write(Utf8JsonWriter writer, SemVersion value, JsonSerializerOptions options) {
    writer.WriteStringValue(value.ToString());
  }
}
