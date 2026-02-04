using Lip.Core.Context;

using System.Reflection;
using System.Text.Json.Serialization;

namespace Lip.Core.Services;

public class ConfigService
{
    private readonly RuntimeConfig _runtimeConfig;
    private readonly IContext _context;
    private readonly IPathManager _pathManager;

    public ConfigService(IContext context)
    {
        _context = context;
        _runtimeConfig = RuntimeConfig.Load(context.FileSystem);
        _pathManager = ServiceFactory.CreatePathManager(context, _runtimeConfig);
    }

    internal ConfigService(RuntimeConfig runtimeConfig, IContext context, IPathManager pathManager)
    {
        _runtimeConfig = runtimeConfig;
        _context = context;
        _pathManager = pathManager;
    }



    public async Task Delete(List<string> keys)
    {
        if (keys.Count == 0)
        {
            throw new ArgumentException("No configuration keys provided.", nameof(keys));
        }

        Dictionary<string, string> allKeyValuePairs = typeof(RuntimeConfig).GetProperties()
            .Where(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>() != null)
            .ToDictionary(
                prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name,
                prop => prop.GetValue(new RuntimeConfig())!.ToString()!
            );

        // Set the configuration keys to their default values.
        Dictionary<string, string> keyValuePairs = keys.ToDictionary(
            key => key,
            key => allKeyValuePairs.GetValueOrDefault(key) ?? throw new ArgumentException($"Unknown configuration key: '{key}'.", nameof(keys))
        );

        await Set(keyValuePairs);
    }

    public Dictionary<string, string> Get(List<string> keys)
    {
        if (keys.Count == 0)
        {
            return List();
        }

        Dictionary<string, string> allKeyValuePairs = List();

        Dictionary<string, string> keyValuePairs = keys.ToDictionary(
            key => key,
            key => allKeyValuePairs.GetValueOrDefault(key) ?? throw new ArgumentException($"Unknown configuration key: '{key}'.", nameof(keys))
        );

        return keyValuePairs;
    }

    public Dictionary<string, string> List()
    {
        Dictionary<string, string> allKeyValuePairs = typeof(RuntimeConfig).GetProperties()
            .Where(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>() != null)
            .ToDictionary(
                prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name,
                prop => prop.GetValue(_runtimeConfig)!.ToString()!
            );

        return allKeyValuePairs;
    }

    public async Task Set(Dictionary<string, string> keyValuePairs)
    {
        if (keyValuePairs.Count == 0)
        {
            throw new ArgumentException("No configuration items provided.", nameof(keyValuePairs));
        }

        // Clone the current runtime configuration to avoid modifying the original one.
        RuntimeConfig newRuntimeConfig = _runtimeConfig with { };

        // Update the configuration with the new values.
        foreach ((string key, string value) in keyValuePairs)
        {
            PropertyInfo matchedProperty = typeof(RuntimeConfig).GetProperties()
                .Where(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>() != null)
                .FirstOrDefault(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name == key)
                ?? throw new ArgumentException($"Unknown configuration key: '{key}'.", nameof(keyValuePairs));

            object convertedValue = Convert.ChangeType(value, matchedProperty.PropertyType);
            matchedProperty.SetValue(newRuntimeConfig, convertedValue);
        }

        _context.FileSystem.CreateParentDirectory(_pathManager.RuntimeConfigPath);

        await _context.FileSystem.File.WriteAllBytesAsync(_pathManager.RuntimeConfigPath, newRuntimeConfig.ToJsonBytes());
    }
}