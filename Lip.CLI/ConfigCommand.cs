using Lip.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

class ConfigSettings : BaseCommandSettings { }

[Description("Delete a configuration key.")]
class ConfigDeleteCommand : AsyncCommand<ConfigDeleteCommand.Settings>
{
    public class Settings : ConfigSettings
    {
        [CommandArgument(0, "<key>")]
        [Description("The configuration key to delete.")]
        public required string Key { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var configService = new ConfigService(ctx);

        await configService.Delete(settings.Key);

        return 0;
    }
}

[Description("Get a configuration value.")]
class ConfigGetCommand : AsyncCommand<ConfigGetCommand.Settings>
{
    public class Settings : ConfigSettings
    {
        [CommandArgument(0, "<key>")]
        [Description("The configuration key to get.")]
        public required string Key { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var configService = new ConfigService(ctx);

        string value = configService.Get(settings.Key);

        AnsiConsole.MarkupLine(value.EscapeMarkup());

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

[Description("Set a configuration value.")]
class ConfigSetCommand : AsyncCommand<ConfigSetCommand.Settings>
{
    public class Settings : ConfigSettings
    {
        [CommandArgument(0, "<key>")]
        [Description("The configuration key to set.")]
        public required string Key { get; init; }

        [CommandArgument(1, "<value>")]
        [Description("The configuration value to set.")]
        public required string Value { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var configService = new ConfigService(ctx);

        await configService.Set(settings.Key, settings.Value);

        return 0;
    }
}