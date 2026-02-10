using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.PublicApi;
using Spectre.Console.Cli;

namespace Lip.Cli.Commands;

public class ListCommand(ILipClient lipClient, IUserInteraction userInteraction) : AsyncCommand<ListCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;
    private readonly IUserInteraction _userInteraction = userInteraction;

    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        (IEnumerable<PackageSpec>? explicitPackages, IEnumerable<PackageSpec>? implicitPackages) = await _lipClient.List();

        await _userInteraction.PrintList("Explicit Packages:", explicitPackages.Select(p => p.ToString()));
        await _userInteraction.PrintInfo(""); // Empty line
        await _userInteraction.PrintList("Implicit Packages:", implicitPackages.Select(p => p.ToString()));

        return 0;
    }
}