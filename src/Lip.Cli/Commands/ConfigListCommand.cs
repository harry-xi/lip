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
        IDictionary<string, string> config = await _lipClient.ConfigList();

        Table table = new();
        table.AddColumn("Key");
        table.AddColumn("Value");

        foreach (KeyValuePair<string, string> kvp in config)
        {
            table.AddRow(kvp.Key, kvp.Value);
        }

        AnsiConsole.Write(table);

        return 0;
    }
}