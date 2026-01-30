using Lip.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

class ConfigSettings : BaseCommandSettings { }

[Description("Delete configuration key(s).")]
class ConfigDeleteCommand : AsyncCommand<ConfigDeleteCommand.Settings>
{
    public class Settings : ConfigSettings
    {
        [CommandArgument(0, "<key ...>")]
        [Description("The configuration keys to delete.")]
        public required string[] Keys { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var configService = new ConfigService(ctx);

        await configService.Delete([.. settings.Keys]);

        return 0;
    }
}

[Description("Get configuration value(s).")]
class ConfigGetCommand : AsyncCommand<ConfigGetCommand.Settings>
{
    public class Settings : ConfigSettings
    {
        [CommandArgument(0, "<key ...>")]
        [Description("The configuration keys to get.")]
        public required string[] Keys { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var configService = new ConfigService(ctx);

        Dictionary<string, string> value = configService.Get([.. settings.Keys]);

        foreach ((string key, string val) in value)
        {
            AnsiConsole.MarkupLine($"{key}={val}".EscapeMarkup());
        }

        return 0;
    }
}

[Description("List all configuration values.")]
class ConfigListCommand : AsyncCommand<ConfigListCommand.Settings>
{
    public class Settings : ConfigSettings { }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var configService = new ConfigService(ctx);

        Dictionary<string, string> value = configService.List();

        foreach ((string key, string val) in value)
        {
            AnsiConsole.MarkupLine($"{key}={val}".EscapeMarkup());
        }

        return 0;
    }
}

[Description("Set configuration value(s).")]
class ConfigSetCommand : AsyncCommand<ConfigSetCommand.Settings>
{
    public class Settings : ConfigSettings
    {
        [CommandArgument(0, "<key=value ...>")]
        [Description("The configuration key-value pairs to set.")]
        public required string[] KeyValuePairs { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var configService = new ConfigService(ctx);

        Dictionary<string, string> entries = [];
        foreach (string pair in settings.KeyValuePairs)
        {
            string[] parts = pair.Split('=', 2);
            if (parts.Length != 2)
            {
                throw new ArgumentException($"Invalid key-value pair format: '{pair}'. Expected format: 'key=value'.");
            }
            entries[parts[0]] = parts[1];
        }

        await configService.Set(entries);

        return 0;
    }
}