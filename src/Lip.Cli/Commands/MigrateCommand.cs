using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

public class MigrateCommand(ILipClient lipClient) : AsyncCommand<MigrateCommand.Settings>
{
    private readonly ILipClient _lipClient = lipClient;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        [Description("The input file to migrate")]
        public required string File { get; init; }

        [CommandArgument(1, "<OUTPUT>")]
        [Description("The output file path")]
        public required string Output { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await _lipClient.Migrate(settings.File, settings.Output);
        AnsiConsole.MarkupLine("[green]Migration completed successfully.[/]");
        return 0;
    }
}