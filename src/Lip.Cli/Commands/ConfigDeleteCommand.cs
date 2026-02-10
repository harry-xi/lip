using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

public class ConfigDeleteCommand(ILipClient lipClient) : AsyncCommand<ConfigDeleteCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("The configuration key to delete")]
        public required string Key { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await _lipClient.ConfigDelete(settings.Key);
        AnsiConsole.MarkupLine($"[green]Config key '{settings.Key}' deleted.[/]");
        return 0;
    }
}