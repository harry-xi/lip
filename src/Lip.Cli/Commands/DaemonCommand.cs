using Lip.Core.PublicApi;
using Lip.Daemon;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.Cli.Commands;

public class DaemonCommand(ILipClient client) : AsyncCommand<DaemonCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Use stdio transport")]
        [CommandOption("--stdio")]
        public bool Stdio { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!settings.Stdio)
        {
            throw new NotSupportedException(
                "Only stdio transport is supported. Please run with --stdio option.");
        }

        using Stream stdin = Console.OpenStandardInput();
        using Stream stdout = Console.OpenStandardOutput();

        var server = new RpcServer(client, stdin, stdout);

        await server.Run();

        return 0;
    }
}
