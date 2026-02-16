using Lip.Cli;
using Lip.Cli.Commands;
using Lip.Core.Infrastructure;
using Lip.Core.PublicApi;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;

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

    config.SetApplicationVersion(SemVersion.Parse(Assembly
        .GetEntryAssembly()?
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion!).ToString());

    config.SetExceptionHandler((ex, resolver) =>
    {
        IAnsiConsole console = AnsiConsole.Create(new()
        {
            Out = new AnsiConsoleOutput(Console.Error)
        });

        if (ex is AggregateException agg)
        {
            console.WriteException(agg, ExceptionFormats.ShortenEverything);
            foreach (Exception inner in agg.InnerExceptions)
            {
                console.WriteException(inner, ExceptionFormats.ShortenEverything);
            }
        }
        else
        {
            console.WriteException(ex, ExceptionFormats.ShortenEverything);
        }

        return 1;
    });

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

    config.AddCommand<VersionsCommand>("versions")
        .WithDescription("Shows available versions for a package");
});

return await app.RunAsync(args);