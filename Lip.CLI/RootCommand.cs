using Lip.CLI;
using Lip.Context;
using Lip.Core;
using Microsoft.Extensions.Logging;
using Semver;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Reflection;

class RootCommand : AsyncCommand<RootCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-q|--quiet")]
        [Description("Show only errors.")]
        public bool Quiet { get; init; }

        [CommandOption("-v|--verbose")]
        [Description("Show verbose output.")]
        public bool Verbose { get; init; }

        [CommandOption("-V|--version")]
        [Description("Show version and exit.")]
        public bool Version { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        (Lip.Core.Lip lip, ILogger logger, UserInteraction userInteraction) = await Prepare(settings);

        if (settings.Version)
        {
            Assembly assembly = Assembly.GetEntryAssembly()!;

            SemVersion version = SemVersion.FromVersion(assembly.GetName().Version!);

            AnsiConsole.MarkupLine($"lip {version} from {assembly.Location}");

            return 0;
        }

        logger.LogCritical("No command specified. Use 'lip --help' for more information.");

        return -1;
    }

    protected static async Task<(Lip.Core.Lip lip, ILogger logger, UserInteraction userInteraction)> Prepare(Settings settings)
    {
        ILogger logger = CreateLogger(settings.Quiet, settings.Verbose);

        RuntimeConfig runtimeConfig = await GetRuntimeConfig();

        UserInteraction userInteraction = new();

        Lip.Core.Lip lip = Lip.Core.Lip.Create(
            runtimeConfig,
            new Context
            {
                CommandRunner = new CommandRunner(),
                Downloader = new Lip.Context.Downloader(userInteraction),
                FileSystem = new FileSystem(),
                Git = await StandaloneGit.Create(),
                Logger = logger,
                UserInteraction = userInteraction,
                WorkingDir = Directory.GetCurrentDirectory()
            }
        );

        return (lip, logger, userInteraction);
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

    private static async Task<RuntimeConfig> GetRuntimeConfig()
    {
        string path = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");

        if (!Path.Exists(path))
        {
            return new RuntimeConfig();
        }

        byte[] json = await File.ReadAllBytesAsync(path);

        return RuntimeConfig.FromJsonBytes(json);
    }
}
