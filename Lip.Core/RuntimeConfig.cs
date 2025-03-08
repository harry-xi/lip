using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core;

/// <summary>
/// Represents the runtime configuration.
/// </summary>
public record RuntimeConfig
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IndentSize = 4,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
    };

    [JsonPropertyName("cache")]
    public string Cache { get; init; } = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "lip", "cache");

    [JsonIgnore]
    public List<string> GitHubProxies
    {
        get => [.. GitHubProxiesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        init => GitHubProxiesText = string.Join(',', value);
    }

    [JsonPropertyName("github_proxies")]
    public string GitHubProxiesText { get; init; } = "";

    [JsonIgnore]
    public List<string> GoModuleProxies
    {
        get => [.. GoModuleProxiesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        init => GoModuleProxiesText = string.Join(',', value);
    }

    [JsonPropertyName("go_module_proxies")]
    public string GoModuleProxiesText { get; init; } = "https://goproxy.io";

    public static RuntimeConfig FromJsonBytes(byte[] bytes)
    {
        try
        {
            var raw = JsonSerializer.Deserialize(
                bytes,
                RawRuntimeConfigJsonContext.Default.RawRuntimeConfig
            ) ?? throw new JsonException("JSON bytes deserialized to null.");
            return new RuntimeConfig
            {
                Cache = raw.Cache ?? Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "lip", "cache"),
                GitHubProxiesText = raw.GitHubProxies ?? "",
                GoModuleProxiesText = raw.GoModuleProxies ?? "https://goproxy.io"
            };
        }
        catch (Exception ex) when (ex is JsonException)
        {
            throw new JsonException("Runtime config bytes deserialization failed.", ex);
        }
    }

    public byte[] ToJsonBytes()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this, RuntimeConfigJsonContext.Default.RuntimeConfig);
    }
}

internal record RawRuntimeConfig
{
    [JsonPropertyName("cache")]
    public string? Cache { get; init; }

    [JsonPropertyName("github_proxies")]
    public string? GitHubProxies { get; init; }

    [JsonPropertyName("go_module_proxies")]
    public string? GoModuleProxies { get; init; }
}

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IndentSize = 4,
    ReadCommentHandling = JsonCommentHandling.Skip,
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(RawRuntimeConfig))]
internal partial class RawRuntimeConfigJsonContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IndentSize = 4,
    ReadCommentHandling = JsonCommentHandling.Skip,
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(RuntimeConfig))]
internal partial class RuntimeConfigJsonContext : JsonSerializerContext
{
}
