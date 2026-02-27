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

        if (ex is AggregateException aggregateException)
        {
            AggregateException flattenedException = aggregateException.Flatten();

            if (flattenedException.InnerExceptions.Count == 1)
            {
                console.WriteException(flattenedException.InnerExceptions[0], ExceptionFormats.ShortenEverything);
            }
            else
            {
                console.MarkupLine($"[red]Unhandled aggregate exception ({flattenedException.InnerExceptions.Count} inner exceptions)[/]");

                for (int index = 0; index < flattenedException.InnerExceptions.Count; index++)
                {
                    console.MarkupLine($"[red]--- Inner #{index + 1} ---[/]");
                    console.WriteException(flattenedException.InnerExceptions[index], ExceptionFormats.ShortenEverything);
                }
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