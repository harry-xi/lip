using Lip.Daemon;
using Lip.Daemon.Commands;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;

ServiceCollection services = new();
TypeRegistrar registrar = new(services);
CommandApp app = new(registrar);

app.Configure(config =>
{
    config.SetApplicationName("lipd");

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

    config.AddCommand<RunCommand>("run")
        .WithDescription("Runs the daemon");
});

return await app.RunAsync(args);