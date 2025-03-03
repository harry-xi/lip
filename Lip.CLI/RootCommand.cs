using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

class RootCommand(Lip.Core.Lip lip, ILogger logger) : AsyncCommand<RootCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--no-color")]
        [Description("Disable colored output.")]
        public bool NoColor { get; init; }
    }

    protected readonly Lip.Core.Lip _lip = lip;
    protected readonly ILogger _logger = logger;

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.NoColor)
        {
            AnsiConsole.WriteLine("Welcome to lip (monochrome mode).");
        }
        else
        {
            AnsiConsole.Write(new Markup("[bold green]Welcome to lip![/]"));
        }
        return Task.FromResult(0);
    }
}
