using Lip.Core.Infrastructure;
using Spectre.Console;

namespace Lip.Cli;

public class ConsoleUserInteraction : IUserInteraction
{
    public async Task PrintError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red]✗ {message}[/]");
    }

    public async Task PrintInfo(string message)
    {
        AnsiConsole.WriteLine(message);
    }

    public async Task PrintSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓ {message}[/]");
    }

    public async Task PrintWarning(string message)
    {
        AnsiConsole.MarkupLine($"[#FFA500]⚠[/] [yellow]{message}[/]");
    }
}