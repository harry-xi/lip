using Lip.Core.Infrastructure;
using Spectre.Console;

namespace Lip.Cli;

public class ConsoleUserInteraction : IUserInteraction
{
    public Task PrintInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue]INFO:[/] {message}");
        return Task.CompletedTask;
    }

    public Task PrintWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]WARNING:[/] {message}");
        return Task.CompletedTask;
    }

    public Task PrintError(string message)
    {
        AnsiConsole.MarkupLine($"[red]ERROR:[/] {message}");
        return Task.CompletedTask;
    }
}