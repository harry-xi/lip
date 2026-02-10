using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Lip.Cli.Commands;

public class ConfigListCommand(ILipClient lipClient) : AsyncCommand<ConfigListCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var config = await _lipClient.ConfigList();

        var table = new Table();
        table.AddColumn("Key");
        table.AddColumn("Value");

        foreach (var kvp in config)
        {
            table.AddRow(kvp.Key, kvp.Value);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}