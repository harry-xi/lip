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



    public async Task Delete(string key)
    {
        Dictionary<string, string> allKeyValuePairs = typeof(RuntimeConfig).GetProperties()
            .Where(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>() != null)
            .ToDictionary(
                prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name,
                prop => prop.GetValue(new RuntimeConfig())!.ToString()!
            );

        string defaultValue = allKeyValuePairs.GetValueOrDefault(key)
            ?? throw new ArgumentException($"Unknown configuration key: '{key}'.", nameof(key));

        await Set(key, defaultValue);
    }

    public string Get(string key)
    {
        Dictionary<string, string> allKeyValuePairs = List();

        return allKeyValuePairs.GetValueOrDefault(key)
            ?? throw new ArgumentException($"Unknown configuration key: '{key}'.", nameof(key));
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

    public async Task Set(string key, string value)
    {
        // Clone the current runtime configuration to avoid modifying the original one.
        RuntimeConfig newRuntimeConfig = _runtimeConfig with { };

        PropertyInfo matchedProperty = typeof(RuntimeConfig).GetProperties()
            .Where(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>() != null)
            .FirstOrDefault(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name == key)
            ?? throw new ArgumentException($"Unknown configuration key: '{key}'.", nameof(key));

        object convertedValue = Convert.ChangeType(value, matchedProperty.PropertyType);
        matchedProperty.SetValue(newRuntimeConfig, convertedValue);

        _context.FileSystem.CreateParentDirectory(_pathManager.RuntimeConfigPath);

        await _context.FileSystem.File.WriteAllBytesAsync(_pathManager.RuntimeConfigPath, newRuntimeConfig.ToJsonBytes());
    }
}