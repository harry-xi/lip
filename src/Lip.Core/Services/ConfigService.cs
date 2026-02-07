using Lip.Core.Entities;
using Lip.Core.Extensions;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Text.Json;

namespace Lip.Core.Services;

public interface IConfigService
{
    Task<RuntimeConfig> LoadConfig();
    Task SaveConfig(RuntimeConfig config);
}

public class ConfigService(IFileSystem fileSystem, ILogger logger) : IConfigService
{
    private static readonly string _configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ILogger _logger = logger;

    public async Task<RuntimeConfig> LoadConfig()
    {
        if (!_fileSystem.File.Exists(_configPath))
        {
            _logger.LogInformation("Runtime configuration file not found at '{ConfigPath}'. Using default configuration.", _configPath);

            RuntimeConfig config = new();

            await SaveConfig(config);

            return config;
        }

        try
        {
            using Stream readStream = _fileSystem.File.OpenRead(_configPath);

            RuntimeConfig config = await JsonSerializer.DeserializeAsync<RuntimeConfig>(readStream, _jsonSerializerOptions)
                ?? throw new JsonException("Failed to deserialize RuntimeConfig");

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load runtime configuration from '{ConfigPath}'.", _configPath);
            _logger.LogInformation("Using default runtime configuration.");

            RuntimeConfig config = new();

            await SaveConfig(config);

            return config;
        }
    }

    public async Task SaveConfig(RuntimeConfig config)
    {
        using Stream writeStream = _fileSystem.CreateFileWithDirectory(_configPath);

        await JsonSerializer.SerializeAsync(writeStream, config, _jsonSerializerOptions);
    }
}