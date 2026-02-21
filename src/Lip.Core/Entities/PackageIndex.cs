using System.Text.Json.Serialization;

namespace Lip.Core.Entities;

public record PackageIndex
{
    private const int _currentFormatVersion = 3;
    private const string _currentFormatUuid = "289f771f-2c9a-4d73-9f3f-8492495a924d";

    [JsonInclude]
    [JsonPropertyName("format_version")]
    [JsonRequired]
    public int FormatVersion
    {
        get => _currentFormatVersion;
        init
        {
            if (value != _currentFormatVersion)
            {
                throw new ArgumentException($"Unsupported format version: {value}", nameof(FormatVersion));
            }
        }
    }

    [JsonInclude]
    [JsonPropertyName("format_uuid")]
    [JsonRequired]
    public string FormatUuid
    {
        get => _currentFormatUuid;
        init
        {
            if (value != _currentFormatUuid)
            {
                throw new ArgumentException($"Unsupported format UUID: {value}", nameof(FormatUuid));
            }
        }
    }

    [JsonPropertyName("packages")]
    public required Dictionary<string, PackageIndexPackage> Packages { get; init; }
}