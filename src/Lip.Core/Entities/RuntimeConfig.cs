using Flurl;
using Lip.Core.Json;
using System.Reflection;
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
                throw new ArgumentException($"Unsupported format version: {value}");
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
                throw new ArgumentException($"Unsupported format UUID: {value}");
            }
        }
    }

    [JsonConverter(typeof(UrlJsonConverter))]
    [JsonPropertyName("github_proxy")]
    public Url? GithubProxy { get; init; }

    [JsonConverter(typeof(UrlJsonConverter))]
    [JsonPropertyName("go_module_proxy")]
    public Url? GoModuleProxy { get; init; }

    public IDictionary<string, dynamic?> AsDictionary()
    {
        return GetType()
            .GetProperties()
            .Where(p => p.GetCustomAttribute<JsonPropertyNameAttribute>() is not null)
            .Select(p => new KeyValuePair<string, dynamic?>(
                p.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name,
                p.GetValue(this)
            ))
            .ToDictionary();
    }

    public RuntimeConfig With(string key, dynamic? value)
    {
        RuntimeConfig newConfig = this with { };

        PropertyInfo prop = GetType().GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == key)
            ?? throw new KeyNotFoundException($"Key not found: {key}");

        prop.SetValue(newConfig, value);

        return newConfig;
    }
}