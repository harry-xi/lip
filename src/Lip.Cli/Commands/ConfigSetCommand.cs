using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

public class ConfigSetCommand(ILipClient lipClient) : AsyncCommand<ConfigSetCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("The config key to set")]
        public required string Key { get; init; }

        [CommandArgument(1, "<VALUE>")]
        [Description("The value to set")]
        public required string Value { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await _lipClient.ConfigSet(settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]Config key '{settings.Key}' set to '{settings.Value}'.[/]");
        return 0;
    }
}