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
        (IEnumerable<PackageSpec> explicitPackages, IEnumerable<PackageSpec> implicitPackages) = await _lipClient.List();

        Tree explicitTree = new("User-Requested");
        foreach (PackageSpec package in explicitPackages)
        {
            explicitTree.AddNode(package.ToString());
        }
        if (!explicitPackages.Any())
        {
            explicitTree.AddNode("[grey italic]None[/]");
        }

        Tree implicitTree = new("Dependencies");
        foreach (PackageSpec package in implicitPackages)
        {
            implicitTree.AddNode(package.ToString());
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