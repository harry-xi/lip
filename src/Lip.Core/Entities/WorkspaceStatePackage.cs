using System.Text.Json.Serialization;

namespace Lip.Core.Entities;

public record WorkspaceStatePackage
{
    [JsonPropertyName("files")]
    public required List<string> Files
    {
        get;
        init
        {
            string? invalidFile = value.FirstOrDefault(f => !PackageManifestAssetPlacement.IsValidDst(f));

            if (invalidFile is not null)
            {
                throw new ArgumentException($"Invalid file path: {invalidFile}");
            }

            field = value;
        }
    }

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
            : throw new ArgumentException($"Invalid package variant: {value}");
    }

    public PackageSpec GetPackageSpec()
    {
        return new PackageSpec(new PackageId(Manifest.Path, Variant), Manifest.Version);
    }
}