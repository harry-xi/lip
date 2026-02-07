using DotNet.Globbing;
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
    public List<Glob> PreserveFiles { get; init; } = [];

    [JsonPropertyName("remove_files")]
    public List<Glob> RemoveFiles { get; init; } = [];

    [JsonPropertyName("scripts")]
    public PackageManifestScripts Scripts { get; init; } = new();
}