using Lip.Core.Infrastructure;
using Lip.Core.PublicApi;
using Spectre.Console.Cli;

namespace Lip.Cli.Commands;

public class ConfigListCommand(ILipClient lipClient, IUserInteraction userInteraction) : AsyncCommand<ConfigListCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;
    private readonly IUserInteraction _userInteraction = userInteraction;

    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        IDictionary<string, string> config = await _lipClient.ConfigList();

        IEnumerable<string> headers = ["Key", "Value"];
        IEnumerable<string[]> rows = config.Select(kvp => new[] { kvp.Key, kvp.Value });

        await _userInteraction.PrintTable(headers, rows);
        return 0;
    }
}