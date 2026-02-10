using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

public class ConfigGetCommand(ILipClient lipClient) : AsyncCommand<ConfigGetCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("The configuration key to get")]
        public required string Key { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        string value = await _lipClient.ConfigGet(settings.Key);
        AnsiConsole.WriteLine(value);
        return 0;
    }
}