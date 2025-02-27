using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip;

public record PackageLock
{
    public record Package
    {
        public required List<string> Files { get; init; }

        public required bool Locked { get; init; }

        public required PackageManifest Manifest { get; init; }

        public PackageSpecifier Specifier => new()
        {
            ToothPath = Manifest.ToothPath,
            VariantLabel = VariantLabel,
            Version = Manifest.Version,
        };

        public required string VariantLabel
        {
            get => _variantLabel;
            init => _variantLabel = StringValidator.CheckVariantLabel(value)
                ? value
                : throw new SchemaViolationException("variant", $"Invalid variant label '{value}'.");
        }
        private string _variantLabel = string.Empty;
    }

    public const int DefaultFormatVersion = 3;
    public const string DefaultFormatUuid = "289f771f-2c9a-4d73-9f3f-8492495a924d";

    public required List<Package> Locks { get; init; }

    public static PackageLock FromJsonBytes(byte[] bytes)
    {
        RawPackageLock rawPackageLock = RawPackageLock.FromJsonBytes(bytes);

        // Validate format version and UUID.

        if (rawPackageLock.FormatVersion != DefaultFormatVersion)
        {
            throw new SchemaViolationException(
                "format_version",
                $"Expected format version {DefaultFormatVersion}, but got {rawPackageLock.FormatVersion}.");
        }

        if (rawPackageLock.FormatUuid != DefaultFormatUuid)
        {
            throw new SchemaViolationException(
                "format_uuid",
                $"Expected format UUID '{DefaultFormatUuid}', but got '{rawPackageLock.FormatUuid}'.");
        }

        return new PackageLock
        {
            Locks = rawPackageLock.Packages
                .ConvertAll(rawPackage => new Package
                {
                    Files = rawPackage.Files,
                    Locked = rawPackage.Locked,
                    Manifest = PackageManifest.FromJsonElement(rawPackage.Manifest),
                    VariantLabel = rawPackage.VariantLabel,
                }),
        };
    }

    public byte[] ToJsonBytes()
    {
        RawPackageLock rawPackageLock = new()
        {
            FormatVersion = DefaultFormatVersion,
            FormatUuid = DefaultFormatUuid,
            Packages = Locks
                .ConvertAll(package => new RawPackageLock.Package
                {
                    Files = package.Files,
                    Locked = package.Locked,
                    Manifest = package.Manifest.ToJsonElement(),
                    VariantLabel = package.VariantLabel,
                }),
        };

        return rawPackageLock.ToJsonBytes();
    }
}

file record RawPackageLock
{
    public record Package
    {
        [JsonPropertyName("files")]
        public required List<string> Files { get; init; }

        [JsonPropertyName("locked")]
        public required bool Locked { get; init; }

        [JsonPropertyName("manifest")]
        public required JsonElement Manifest { get; init; }

        [JsonPropertyName("variant")]
        public required string VariantLabel { get; init; }
    }

    [JsonPropertyName("format_version")]
    public required int FormatVersion { get; init; }

    [JsonPropertyName("format_uuid")]
    public required string FormatUuid { get; init; }

    [JsonPropertyName("packages")]
    public required List<Package> Packages { get; init; }

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IndentSize = 4,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
    };

    public static RawPackageLock FromJsonBytes(byte[] bytes)
    {
        try
        {
            return JsonSerializer.Deserialize<RawPackageLock>(
                bytes,
                s_jsonSerializerOptions)
                ?? throw new SchemaViolationException("", "JSON bytes deserialized to null.");
        }
        catch (Exception ex) when (ex is JsonException)
        {
            throw new JsonException("Package lock bytes deserialization failed.", ex);
        }
    }

    public byte[] ToJsonBytes()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this, s_jsonSerializerOptions);
    }
}
