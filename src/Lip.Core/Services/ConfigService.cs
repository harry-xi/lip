using Lip.Core.Entities;
using Lip.Core.Json;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lip.Core.Services;

public interface IConfigService
{
    Task Delete(string key);
    Task<string> Get(string key);
    Task<IDictionary<string, string>> List();
    Task Set(string key, string value);
}

public class ConfigService(IFileSystem fileSystem, ILogger logger) : IConfigService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new UrlJsonConverter() }
    };

    private readonly string _configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");

    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ILogger _logger = logger;

    public async Task Delete(string key)
    {
        RuntimeConfig config = await LoadConfig();
        PropertyInfo? property = GetPropertyByJsonName(key);

        if (property == null)
        {
            throw new KeyNotFoundException($"Configuration key '{key}' not found");
        }

        if (!property.CanWrite)
        {
            throw new InvalidOperationException($"Configuration key '{key}' is read-only");
        }

        if (property.Name is "FormatVersion" or "FormatUuid")
        {
            throw new InvalidOperationException($"Configuration key '{key}' cannot be modified");
        }

        if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
        {
            throw new InvalidOperationException($"Configuration key '{key}' cannot be deleted (non-nullable)");
        }

        RuntimeConfig updatedConfig = config with { };

        try
        {
            property.SetValue(updatedConfig, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }

        await SaveConfig(updatedConfig);
    }

    public async Task<string> Get(string key)
    {
        RuntimeConfig config = await LoadConfig();
        PropertyInfo? property = GetPropertyByJsonName(key);

        if (property == null)
        {
            throw new KeyNotFoundException($"Configuration key '{key}' not found");
        }

        object? value = property.GetValue(config);
        return value?.ToString() ?? string.Empty;
    }

    public async Task<IDictionary<string, string>> List()
    {
        RuntimeConfig config = await LoadConfig();

        Dictionary<string, string> result = new();

        PropertyInfo[] properties = typeof(RuntimeConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            JsonPropertyNameAttribute? jsonAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonAttr != null)
            {
                object? value = property.GetValue(config);
                if (value != null)
                {
                    result[jsonAttr.Name] = value.ToString() ?? string.Empty;
                }
            }
        }

        return result;
    }

    public async Task Set(string key, string value)
    {
        RuntimeConfig config = await LoadConfig();
        PropertyInfo? property = GetPropertyByJsonName(key);

        if (property == null)
        {
            throw new KeyNotFoundException($"Configuration key '{key}' not found");
        }

        if (!property.CanWrite)
        {
            throw new InvalidOperationException($"Configuration key '{key}' is read-only");
        }

        if (property.Name is "FormatVersion" or "FormatUuid")
        {
            throw new InvalidOperationException($"Configuration key '{key}' cannot be modified");
        }

        object? convertedValue;
        Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        try
        {
            if (targetType == typeof(Flurl.Url))
            {
                convertedValue = new Flurl.Url(value);
            }
            else
            {
                convertedValue = Convert.ChangeType(value, targetType);
            }

            RuntimeConfig updatedConfig = config with { };
            property.SetValue(updatedConfig, convertedValue);

            await SaveConfig(updatedConfig);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private async Task<RuntimeConfig> LoadConfig()
    {
        try
        {
            using Stream readStream = _fileSystem.File.OpenRead(_configPath);

            RuntimeConfig config = await JsonSerializer.DeserializeAsync<RuntimeConfig>(readStream, _jsonOptions)
                ?? throw new JsonException("Failed to deserialize RuntimeConfig");

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load runtime configuration from {ConfigPath}", _configPath);
            _logger.LogInformation("Using default runtime configuration");

            RuntimeConfig config = new();

            await SaveConfig(config);

            return config;
        }
    }

    private async Task SaveConfig(RuntimeConfig config)
    {
        using Stream writeStream = _fileSystem.File.Create(_configPath);
        await JsonSerializer.SerializeAsync(writeStream, config, _jsonOptions);
    }

    private static PropertyInfo? GetPropertyByJsonName(string jsonName)
    {
        PropertyInfo[] properties = typeof(RuntimeConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            JsonPropertyNameAttribute? jsonAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonAttr != null && jsonAttr.Name == jsonName)
            {
                return property;
            }
        }

        return null;
    }
}