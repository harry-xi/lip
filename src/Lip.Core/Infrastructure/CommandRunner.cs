using CliWrap;

namespace Lip.Core.Infrastructure;

public interface ICommandRunner
{
    Task Run(string command);
}

public class CommandRunner : ICommandRunner
{
    public async Task Run(string command)
    {
        using Stream stdErrStream = Console.OpenStandardError();

        CommandResult result = await Cli.Wrap(OperatingSystem.IsWindows() ? "cmd.exe" : "sh")
            .WithArguments([
                OperatingSystem.IsWindows() ? "/c" : "-c",
                command
            ])
            .WithStandardInputPipe(PipeSource.Null)
            .WithStandardOutputPipe(PipeTarget.ToStream(stdErrStream))
            .WithStandardErrorPipe(PipeTarget.ToStream(stdErrStream))
            .ExecuteAsync();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Command failed with exit code {result.ExitCode}");
        }
    }
}