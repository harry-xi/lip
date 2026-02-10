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

    public async Task PrintTable(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        await Task.Run(() =>
        {
            var table = new Table();
            foreach (var header in headers)
            {
                table.AddColumn(header);
            }

            foreach (var row in rows)
            {
                table.AddRow(row.ToArray());
            }

            AnsiConsole.Write(table);
        });
    }

    public async Task PrintList(string header, IEnumerable<string> items)
    {
        await Task.Run(() =>
        {
            AnsiConsole.MarkupLine($"[bold]{header}[/]");
            foreach (var item in items)
            {
                AnsiConsole.MarkupLine($"  - {item}");
            }
        });
    }
}