using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

class RootCommand(Lip.Core.Lip lip, ILogger logger) : AsyncCommand<RootCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    protected readonly Lip.Core.Lip _lip = lip;
    protected readonly ILogger _logger = logger;

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await Task.Delay(0);
        return 0;
    }
}
