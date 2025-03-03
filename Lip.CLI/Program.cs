using Lip.CLI;
using Lip.Context;
using Lip.Core;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = factory.CreateLogger<Program>();

var userInteraction = new UserInteraction();

var context = new Context()
{
    CommandRunner = new CommandRunner(),
    Downloader = new Lip.Context.Downloader(userInteraction),
    FileSystem = new FileSystem(),
    Git = await StandaloneGit.Create(),
    Logger = logger,
    UserInteraction = userInteraction,
    WorkingDir = Directory.GetCurrentDirectory()
};

var runtimeConfig = new RuntimeConfig();

var lip = Lip.Core.Lip.Create(runtimeConfig, context);

try
{
    await lip.Init(new());
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Failed to initialize Lip");
}
