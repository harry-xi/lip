using Lip.Core;
using Lip.GUI.Lite.Models;
using System.IO;
using System.Text.Json;

namespace Lip.GUI.Lite.Services
{
    public class ConfigService(string? configPath = null)
    {
        static JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

        private readonly string _configPath = configPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "lipui",
                "appconfig.json"
            );
        private readonly string _runtimeConfigPath = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");

        public AppConfig Config { get; private set; } = null!;
        public RuntimeConfig RuntimeConfig { get; set; } = null!;

        public async Task Load()
        {
            if (!File.Exists(_configPath))
            {
                Config = AppConfig.Default;
                await Save();
                return;
            }

            var json = await File.ReadAllTextAsync(_configPath);
            Config = JsonSerializer.Deserialize<AppConfig>(json, SerializerOptions) ?? AppConfig.Default;
        }

        public async Task LoadRuntimeConfig()
        {

            if (!Path.Exists(_runtimeConfigPath))
            {
                RuntimeConfig = new RuntimeConfig();
            }

            byte[] json = await File.ReadAllBytesAsync(_runtimeConfigPath);

            RuntimeConfig = RuntimeConfig.FromJsonBytes(json);
        }

        public async Task SaveRuntimeConfig()
        {
            var dir = Path.GetDirectoryName(_runtimeConfigPath)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllBytesAsync(_runtimeConfigPath, RuntimeConfig.ToJsonBytes());


        }

        public void UpdateRuntimeConfig(Func<RuntimeConfig, RuntimeConfig> updater)
        {
            RuntimeConfig = updater(RuntimeConfig);
        }

        public async Task Save()
        {
            var dir = Path.GetDirectoryName(_configPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(Config, SerializerOptions);
            await File.WriteAllTextAsync(_configPath, json);
        }
    }
}