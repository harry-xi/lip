using CliWrap;
using System.Text;

namespace Lip.Core.Infrastructure;

public interface IGitRunner
{
    Task Clone(
        string repo,
        string? dir = null,
        string? branch = null,
        int? depth = null);

    Task<IEnumerable<(string Sha, string Ref)>> LsRemote(
        string repository,
        bool refs = false,
        bool tags = false);
}

public class GitRunner : IGitRunner
{
    public async Task Clone(
        string repo,
        string? dir = null,
        string? branch = null,
        int? depth = null)
    {
        using Stream stdErrStream = Console.OpenStandardError();

        await Cli.Wrap("git")
            .WithArguments(
            [
                "clone",
                .. (branch is not null) ? new[] { "--branch", branch } : [],
                .. (depth is not null) ? new[] { "--depth", depth.ToString()! } : [],
                "--",
                repo,
                .. (dir is not null) ? new[] { dir } : []
            ])
            .WithStandardInputPipe(PipeSource.Null)
            .WithStandardOutputPipe(PipeTarget.ToStream(stdErrStream))
            .WithStandardErrorPipe(PipeTarget.ToStream(stdErrStream))
            .ExecuteAsync();
    }

    public async Task<IEnumerable<(string Sha, string Ref)>> LsRemote(
        string repository,
        bool refs = false,
        bool tags = false)
    {
        using MemoryStream outStream = new();
        using Stream stdErrStream = Console.OpenStandardError();

        await Cli.Wrap("git")
            .WithArguments([
                "ls-remote",
                .. refs ? new List<string> { "--refs" } : [],
                .. tags ? new List<string> { "--tags" } : [],
                repository
            ])
            .WithStandardInputPipe(PipeSource.Null)
            .WithStandardOutputPipe(PipeTarget.ToStream(outStream))
            .WithStandardErrorPipe(PipeTarget.ToStream(stdErrStream))
            .ExecuteAsync();

        string outputString = Encoding.UTF8.GetString(outStream.ToArray());

        return outputString
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static line =>
            {
                string[] parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return (Sha: parts[0], Ref: parts[1]);
            });
    }
}