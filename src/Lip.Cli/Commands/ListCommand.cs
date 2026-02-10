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
        (IEnumerable<PackageSpec>? explicitPackages, IEnumerable<PackageSpec>? implicitPackages) = await _lipClient.List();

        AnsiConsole.MarkupLine("[bold]Explicit Packages:[/]");
        foreach (PackageSpec pkg in explicitPackages)
        {
            AnsiConsole.MarkupLine($"  - {pkg}");
        }

        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Implicit Packages:[/]");
        foreach (PackageSpec pkg in implicitPackages)
        {
            AnsiConsole.MarkupLine($"  - {pkg}");
        }

        return 0;
    }
}