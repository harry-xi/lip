using Flurl;
using System.Text.Json.Serialization;

namespace Lip.Core.Entities;

public record RuntimeConfig
{
    private const int _currentFormatVersion = 3;
    private const string _currentFormatUuid = "289f771f-2c9a-4d73-9f3f-8492495a924d";

    [JsonInclude]
    [JsonRequired]
    [JsonPropertyName("format_version")]
    public int FormatVersion
    {
        get => _currentFormatVersion;
        init
        {
            if (value != _currentFormatVersion)
            {
                throw new NotSupportedException($"Unsupported format version: {value}");
            }
        }
    }

    [JsonInclude]
    [JsonRequired]
    [JsonPropertyName("format_uuid")]
    public string FormatUuid
    {
        get => _currentFormatUuid;
        init
        {
            if (value != _currentFormatUuid)
            {
                throw new NotSupportedException($"Unsupported format UUID: {value}");
            }
        }
    }

    [JsonPropertyName("github_proxy")]
    public Url? GithubProxy { get; init; }

    [JsonPropertyName("go_module_proxy")]
    public Url? GoModuleProxy { get; init; }
}