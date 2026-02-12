using Lip.Cli;
using Lip.Cli.Commands;

using Lip.Core.Infrastructure;
using Lip.Core.PublicApi;
using Spectre.Console;
using Spectre.Console.Cli;

LipClient lipClient = await LipClient.Create(new ConsoleUserInteraction());
// Register ConsoleUserInteraction as IUserInteraction
ConsoleUserInteraction userInteraction = new();

TypeRegistrar registrar = new();
registrar.RegisterInstance(typeof(ILipClient), lipClient);
registrar.RegisterInstance(typeof(IUserInteraction), userInteraction);

CommandApp app = new(registrar);

app.Configure(config =>
{
    config.SetApplicationName("lip");

    config.AddBranch("cache", cache =>
    {
        cache.AddCommand<CacheCleanCommand>("clean")
            .WithDescription("Cleans the local cache");
    });

    config.AddBranch("config", config =>
    {
        config.AddCommand<ConfigGetCommand>("get")
            .WithDescription("Gets a configuration value");
        config.AddCommand<ConfigSetCommand>("set")
            .WithDescription("Sets a configuration value");
        config.AddCommand<ConfigListCommand>("list")
            .WithDescription("Lists all configuration values");
        config.AddCommand<ConfigDeleteCommand>("delete")
            .WithDescription("Deletes a configuration value");
    });

    config.AddCommand<DaemonCommand>("daemon")
        .WithDescription("Starts the JSON-RPC daemon");

    config.AddCommand<InitCommand>("init")
        .WithDescription("Initializes a new project");

    config.AddCommand<InstallCommand>("install")
        .WithDescription("Installs packages");

    config.AddCommand<ListCommand>("list")
        .WithDescription("Lists installed packages");

    config.AddCommand<MigrateCommand>("migrate")
        .WithDescription("Migrates a package manifest");

    config.AddCommand<UninstallCommand>("uninstall")
        .WithDescription("Uninstalls packages");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription("Updates packages");

    config.AddCommand<ViewCommand>("view")
        .WithDescription("Views package details");

    config.AddCommand<VersionCommand>("version")
        .WithDescription("Shows version information");

    config.PropagateExceptions();
});

try
{
    return await app.RunAsync(args);
}
catch (AggregateException ex)
{
    AnsiConsole.WriteException(ex,
        ExceptionFormats.ShortenEverything);

    foreach (var inner in ex.InnerExceptions)
    {
        AnsiConsole.WriteException(inner,
            ExceptionFormats.ShortenEverything);
    }

    return -1;
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex,
        ExceptionFormats.ShortenEverything);
    return -1;
}