using System.Text.Json.Serialization;

namespace Lip.Core.Entities;

public record PackageIndexPackage
{
    [JsonPropertyName("info")]
    public required PackageManifestInfo Info { get; init; }

    [JsonPropertyName("stargazer_count")]
    public required int StargazerCount { get; init; }

    [JsonPropertyName("updated_at")]
    public required DateTime UpdatedAt { get; init; }

    [JsonPropertyName("variants")]
    public required Dictionary<string, PackageIndexVariant> Variants
    {
        get;
        init => field = value.Keys.All(PackageId.IsValidVariant)
            ? value
            : throw new FormatException($"Invalid variant: {value.First(kv => !PackageId.IsValidVariant(kv.Key)).Key}");
    }
}