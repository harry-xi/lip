using Lip.CLI;
using Lip.Context;
using Lip.Core;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.IO.Abstractions;

var logger = CreateLogger();

var runtimeConfig = await GetRuntimeConfig();

var userInteraction = new UserInteraction();

var lip = Lip.Core.Lip.Create(
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

var commandApp = new CommandApp<RootCommand>();

#pragma warning disable CS4014
userInteraction.RunProgressService();
#pragma warning restore CS4014

try
{
    return await commandApp.RunAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return -1;
}

static ILogger CreateLogger()
{
    using var factory = LoggerFactory.Create(builder => builder.AddConsole());
    return factory.CreateLogger<Program>();
}

static async Task<RuntimeConfig> GetRuntimeConfig()
{
    var path = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");

    if (!Path.Exists(path))
    {
        return new RuntimeConfig();
    }

    var json = await File.ReadAllBytesAsync(path);

    return RuntimeConfig.FromJsonBytes(json);
}
