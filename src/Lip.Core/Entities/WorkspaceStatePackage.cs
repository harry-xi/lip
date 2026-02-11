using System.Text.Json.Serialization;

namespace Lip.Core.Entities;

public record WorkspaceStatePackage
{
    [JsonPropertyName("files")]
    public required List<string> Files { get; init; }

    [JsonPropertyName("locked")]
    public required bool IsExplicit { get; init; }

    [JsonPropertyName("manifest")]
    public required PackageManifest Manifest { get; init; }

    [JsonPropertyName("variant")]
    public required string Variant
    {
        get;
        init => field = PackageId.IsValidVariant(value)
            ? value
            : throw new ArgumentException($"Invalid package variant: {value}", nameof(Variant));
    }

    public PackageSpec GetPackageSpec()
    {
        return new PackageSpec(new PackageId(Manifest.Path, Variant), Manifest.Version);
    }
}