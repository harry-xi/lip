using Lip.Core.Entities;
using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Lip.Cli.Commands;

public class ListCommand(ILipClient lipClient) : AsyncCommand<ListCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        (IEnumerable<string> explicitPackages, IEnumerable<string> implicitPackages) = await _lipClient.List();

        Tree explicitTree = new("User-Requested");
        foreach (string package in explicitPackages)
        {
            explicitTree.AddNode(package);
        }
        if (!explicitPackages.Any())
        {
            explicitTree.AddNode("[grey italic]None[/]");
        }

        Tree implicitTree = new("Dependencies");
        foreach (string package in implicitPackages)
        {
            implicitTree.AddNode(package);
        }
        if (!implicitPackages.Any())
        {
            implicitTree.AddNode("[grey italic]None[/]");
        }

        AnsiConsole.Write(explicitTree);
        AnsiConsole.Write(implicitTree);

        return 0;
    }
}