using Flurl;
using System.Text.Json.Serialization;

namespace Lip.Core;

public partial record PackageManifest
{
    public record Asset
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum TypeEnum
        {
            [JsonStringEnumMemberName("self")]
            Self,
            [JsonStringEnumMemberName("tar")]
            Tar,
            [JsonStringEnumMemberName("tgz")]
            Tgz,
            [JsonStringEnumMemberName("uncompressed")]
            Uncompressed,
            [JsonStringEnumMemberName("zip")]
            Zip,
        }

        [JsonPropertyName("type")]
        public required TypeEnum Type { get; init; }

        [JsonPropertyName("urls")]
        public List<Url> Urls { get; init; } = [];

        [JsonPropertyName("placements")]
        public List<Placement> Placements { get; init; } = [];
    }
}