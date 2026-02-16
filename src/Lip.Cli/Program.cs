using Lip.Cli;
using Lip.Cli.Commands;
using Lip.Core.Infrastructure;
using Lip.Core.PublicApi;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

ConsoleUserInteraction userInteraction = new();

LipClient lipClient = await LipClient.Create(userInteraction);

ServiceCollection services = new();

services.AddSingleton<ILipClient>(lipClient);
services.AddSingleton<IUserInteraction>(userInteraction);

TypeRegistrar registrar = new(services);

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

    config.AddCommand<VersionsCommand>("versions")
        .WithDescription("Shows available versions for a package");

    config.SetExceptionHandler((ex, resolver) =>
    {
        if (ex is AggregateException agg)
        {
            AnsiConsole.WriteException(agg, ExceptionFormats.ShortenEverything);
            foreach (var inner in agg.InnerExceptions)
            {
                AnsiConsole.WriteException(inner, ExceptionFormats.ShortenEverything);
            }
        }
        else
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }

        return 1;
    });
});

return await app.RunAsync(args);