using System.IO.Abstractions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Lip;

public partial class Lip
{
    public record ConfigSetArgs
    {
        public List<Tuple<string, string>> ConfigItems { get; init; } = [];
    }

    private const string RuntimeConfigFileName = ".liprc";

    public async Task ConfigSet(ConfigSetArgs args)
    {
        // Clone the current runtime configuration to avoid modifying the original one.
        RuntimeConfig newRuntimeConfig = _runtimeConfig with { };

        // Update the configuration with the new values.
        foreach ((string key, string value) in args.ConfigItems)
        {
            PropertyInfo[] properties = typeof(RuntimeConfig).GetProperties();

            PropertyInfo? matchedProperty = properties.FirstOrDefault(prop =>
            {
                var attr = prop.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                    .FirstOrDefault() as JsonPropertyNameAttribute;
                return attr?.Name == key;
            }) ?? throw new ArgumentException($"Unknown configuration key: {key}.", nameof(args));

            object convertedValue = Convert.ChangeType(value, matchedProperty.PropertyType);
            matchedProperty.SetValue(newRuntimeConfig, convertedValue);
        }

        await CreateOrUpdateRuntimeConfigurationFile(_fileSystem, newRuntimeConfig);
    }

    private static async Task CreateOrUpdateRuntimeConfigurationFile(IFileSystem fileSystem, RuntimeConfig runtimeConfig)
    {
        string runtimeConfigPath = fileSystem.Path.Join(
            fileSystem.Directory.GetCurrentDirectory(), RuntimeConfigFileName);

        if (!fileSystem.File.Exists(runtimeConfigPath))
        {
            await fileSystem.File.WriteAllBytesAsync(runtimeConfigPath, runtimeConfig.ToBytes());
        }
    }
}
