using Lip.Core.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.Json;

public class PackageIdJsonConverter : JsonConverter<PackageId>
{
    public override PackageId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? s = reader.GetString();
        return s is null ? null : PackageId.Parse(s);
    }

    public override void Write(Utf8JsonWriter writer, PackageId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    public override PackageId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? s = reader.GetString();
        return s is null ? throw new JsonException() : PackageId.Parse(s);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, PackageId value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }
}