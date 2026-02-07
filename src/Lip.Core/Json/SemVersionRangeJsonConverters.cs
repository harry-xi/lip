using Semver;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.Json;

public class SemVersionRangeJsonConverter : JsonConverter<SemVersionRange>
{
    public override SemVersionRange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? s = reader.GetString();
        return s is null ? null : SemVersionRange.Parse(s);
    }

    public override void Write(Utf8JsonWriter writer, SemVersionRange value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}