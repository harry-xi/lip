using Spectre.Console.Cli;

var commandApp = new CommandApp();

commandApp.SetDefaultCommand<CommandRoot>();

commandApp.Configure(config =>
{
    config.SetApplicationName("lip");

#if DEBUG
    config.PropagateExceptions();
#endif

    config.AddCommand<PackCommand>("pack");
    config.AddCommand<PruneCommand>("prune");
    config.AddCommand<RunCommand>("run");
    config.AddCommand<UninstallCommand>("uninstall");
    config.AddCommand<UpdateCommand>("update");
    config.AddCommand<ViewCommand>("view");
});

return await commandApp.RunAsync(args);
