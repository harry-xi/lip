using System.Text.Json.Serialization;

namespace Lip.Core;

public partial record PackageManifest
{
    public record Placement
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum TypeEnum
        {
            [JsonStringEnumMemberName("file")]
            File,
            [JsonStringEnumMemberName("dir")]
            Dir,
        }

        [JsonPropertyName("type")]
        public required TypeEnum Type { get; init; }

        [JsonPropertyName("src")]
        public required string Src { get; init; }

        [JsonPropertyName("dest")]
        public required string Dest
        {
            get;
            init => field = IsValidPlacementDest(value)
                ? value
                : throw new SchemaViolationException("placements[].dest", $"Invalid destination path '{value}'.");
        }
    }
}