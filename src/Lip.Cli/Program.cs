using Lip.Cli;
using Lip.Cli.Commands;

using Lip.Core.PublicApi;
using Spectre.Console.Cli;

var lipClient = await LipClient.Create(new ConsoleUserInteraction());

var registrar = new TypeRegistrar();
registrar.RegisterInstance(typeof(ILipClient), lipClient);

var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("lip");

    config.AddBranch("cache", cache =>
    {
        cache.AddCommand<CacheCleanCommand>("clean")
            .WithDescription("Cleans the cache");
    });

    config.AddBranch("config", config =>
    {
        config.AddCommand<ConfigGetCommand>("get")
            .WithDescription("Get a configuration value");
        config.AddCommand<ConfigSetCommand>("set")
            .WithDescription("Set a configuration value");
        config.AddCommand<ConfigListCommand>("list")
            .WithDescription("List configuration values");
        config.AddCommand<ConfigDeleteCommand>("delete")
            .WithDescription("Delete a configuration value");
    });

    config.AddCommand<InitCommand>("init")
        .WithDescription("Initialize a new Lip project");

    config.AddCommand<InstallCommand>("install")
        .WithDescription("Install packages");

    config.AddCommand<ListCommand>("list")
        .WithDescription("List installed packages");

    config.AddCommand<MigrateCommand>("migrate")
        .WithDescription("Migrate a package manifest");

    config.AddCommand<UninstallCommand>("uninstall")
        .WithDescription("Uninstall packages");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription("Update packages");

    config.AddCommand<ViewCommand>("view")
        .WithDescription("View package details");
});

return await app.RunAsync(args);