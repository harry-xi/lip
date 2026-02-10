using Lip.Core.Infrastructure;
using Lip.Core.PublicApi;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

public class ConfigGetCommand(ILipClient lipClient, IUserInteraction userInteraction) : AsyncCommand<ConfigGetCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;
    private readonly IUserInteraction _userInteraction = userInteraction;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("The configuration key to get")]
        public required string Key { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        string value = await _lipClient.ConfigGet(settings.Key);
        await _userInteraction.PrintInfo(value);
        return 0;
    }
}