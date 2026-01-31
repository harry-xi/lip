using Lip.Core;
using Lip.Core.Context;
using Microsoft.Extensions.Logging;
using Semver;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Reflection;

namespace Lip.CLI;

[Description("lip is a general package manager.")]
class CommandRoot : AsyncCommand<CommandRoot.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandOption("-V|--version")]
        [Description("Show version and exit.")]
        public required bool Version { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CreateContext(settings);

        if (settings.Version)
        {
            SemVersion version = SemVersion.FromVersion(Assembly.GetEntryAssembly()!.GetName().Version!);

            AnsiConsole.MarkupLine(version.ToString().EscapeMarkup());

            return 0;
        }

        ctx.Logger.LogCritical("No command specified. Use 'lip --help' for more information.");

        return 0;
    }

    public static async Task<IContext> CreateContext(
        BaseCommandSettings settings,
        bool doNotRunProgressService = false)
    {
        ILogger logger = CreateLogger(settings.Quiet, settings.Verbose);


        UserInteraction userInteraction = new();

        IContext context = new Context
        {
            CommandRunner = new CommandRunner(),
            Downloader = new Lip.Core.Context.Downloader(userInteraction),
            FileSystem = new FileSystem(),
            Git = await Git.Create(),
            Logger = logger,
            UserInteraction = userInteraction,
            WorkingDir = Directory.GetCurrentDirectory()
        };

        if (!doNotRunProgressService)
        {
            _ = userInteraction.RunProgressService();
        }

        return context;
    }

    private static ILogger CreateLogger(bool quiet, bool verbose)
    {
        LogLevel logLevel = quiet
            ? LogLevel.Error
            : verbose
                ? LogLevel.Debug
                : LogLevel.Information;

        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                })
                .SetMinimumLevel(logLevel);
        });
        return factory.CreateLogger("lip");
    }

}