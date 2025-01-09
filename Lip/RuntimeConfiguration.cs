using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip;

public record RuntimeConfiguration
{
    private static readonly string s_defaultCache = OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "lip-cache")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "lip");

    private static readonly string s_defaultScriptShell = OperatingSystem.IsWindows()
        ? "cmd.exe"
        : "/bin/sh";

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IndentSize = 4,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
    };

    [JsonPropertyName("cache")]
    public string Cache { get; init; } = s_defaultCache;

    [JsonPropertyName("color")]
    public bool Color { get; init; } = true;

    [JsonPropertyName("git")]
    public string Git { get; init; } = "git";

    [JsonPropertyName("github_proxy")]
    public string GitHubProxy { get; init; } = "";

    [JsonPropertyName("go_module_proxy")]
    public string GoModuleProxy { get; init; } = "https://goproxy.io";

    [JsonPropertyName("https_proxy")]
    public string HttpsProxy { get; init; } = "";

    [JsonPropertyName("noproxy")]
    public string NoProxy { get; init; } = "";

    [JsonPropertyName("proxy")]
    public string Proxy { get; init; } = "";

    [JsonPropertyName("script_shell")]
    public string ScriptShell { get; init; } = s_defaultScriptShell;

    public static RuntimeConfiguration FromBytes(byte[] bytes)
    {
        return JsonSerializer.Deserialize<RuntimeConfiguration>(
            bytes,
            s_jsonSerializerOptions
        ) ?? throw new ArgumentException("Failed to deserialize runtime configuration.", nameof(bytes));
    }

    public byte[] ToBytes()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this, s_jsonSerializerOptions);
    }
}
