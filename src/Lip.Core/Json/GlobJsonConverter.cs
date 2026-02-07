using DotNet.Globbing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.Json;

public class GlobJsonConverter : JsonConverter<Glob>
{
    public override Glob? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? s = reader.GetString();
        return s is null ? null : Glob.Parse(s);
    }

    public override void Write(Utf8JsonWriter writer, Glob value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}