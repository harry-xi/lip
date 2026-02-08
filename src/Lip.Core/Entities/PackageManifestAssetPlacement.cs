using DotNet.Globbing;
using System.Text.Json.Serialization;

namespace Lip.Core.Entities;

public record PackageManifestAssetPlacement
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PlacementType
    {
        [JsonStringEnumMemberName("file")]
        File,
        [JsonStringEnumMemberName("dir")]
        Directory,
    }

    [JsonPropertyName("type")]
    public required PlacementType Type { get; init; }

    [JsonPropertyName("src")]
    public required string Src { get; init; }

    [JsonPropertyName("dst")]
    public required string Dst
    {
        get;
        init => field = IsValidDst(value)
            ? value
            : throw new ArgumentException($"Invalid destination path: {value}");
    }

    public static bool IsValidDst(string path) => !(Path.IsPathFullyQualified(path) || Path.IsPathRooted(path) || path.Contains(".."));
}