using Lip.Core.Infrastructure;
using Lip.Core.PublicApi;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

public class ViewCommand(ILipClient lipClient, IUserInteraction userInteraction) : AsyncCommand<ViewCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;
    private readonly IUserInteraction _userInteraction = userInteraction;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<PACKAGE>")]
        [Description("The package to view")]
        public required string Package { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var json = await _lipClient.View(settings.Package);
        await _userInteraction.PrintInfo(json);
        return 0;
    }
}