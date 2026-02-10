using Lip.Core.Entities;
using Lip.Core.Infrastructure;

using System.IO.Abstractions;
using System.Text.Json;

namespace Lip.Core.Services;

public interface IConfigService
{
    Task<RuntimeConfig> LoadConfig();
    Task SaveConfig(RuntimeConfig config);
}

public class ConfigService(IFileSystem fileSystem, IUserInteraction userInteraction) : IConfigService
{
    private static readonly string _configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IUserInteraction _userInteraction = userInteraction;

    public async Task<RuntimeConfig> LoadConfig()
    {
        if (!_fileSystem.File.Exists(_configPath))
        {
            await _userInteraction.PrintWarning(
                $"Runtime configuration file not found at '{_configPath}'. Using default configuration.");

            RuntimeConfig config = new();

            await SaveConfig(config);

            return config;
        }

        try
        {
            using Stream readStream = _fileSystem.File.OpenRead(_configPath);

            RuntimeConfig config = (await JsonSerializer.DeserializeAsync<RuntimeConfig>(readStream, _jsonSerializerOptions))!;

            return config;
        }
        catch (Exception ex)
        {
            await _userInteraction.PrintError($"Failed to load runtime configuration from '{_configPath}': {ex.Message}");
            await _userInteraction.PrintWarning("Using default runtime configuration.");

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