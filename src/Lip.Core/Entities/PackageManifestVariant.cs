using Semver;
using System.Text.Json.Serialization;

namespace Lip.Core.Entities;

public record PackageManifestVariant
{
    [JsonPropertyName("label")]
    public string Label { get; init; } = "";

    [JsonPropertyName("platform")]
    public string Platform { get; init; } = "";

    [JsonPropertyName("dependencies")]
    public Dictionary<PackageId, SemVersionRange> Dependencies { get; init; } = [];

    [JsonPropertyName("assets")]
    public List<PackageManifestAsset> Assets { get; init; } = [];

    [JsonPropertyName("preserve_files")]
    public List<string> PreserveFiles
    {
        get;
        init
        {
            string? invalidPath = value.FirstOrDefault(path => !PackageManifestAssetPlacement.IsValidDst(path));
            field = (invalidPath == null)
                ? value
                : throw new ArgumentException($"Invalid preserve file path: {invalidPath}");
        }
    } = [];

    [JsonPropertyName("remove_files")]
    public List<string> RemoveFiles
    {
        get;
        init
        {
            string? invalidPath = value.FirstOrDefault(path => !PackageManifestAssetPlacement.IsValidDst(path));
            field = (invalidPath == null)
                ? value
                : throw new ArgumentException($"Invalid remove file path: {invalidPath}");
        }
    } = [];

    [JsonPropertyName("scripts")]
    public PackageManifestScripts Scripts { get; init; } = new();
}