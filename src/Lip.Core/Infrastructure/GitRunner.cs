using System.IO.Abstractions;

namespace Lip.Core.Infrastructure;

public interface IGitRunner
{
    Task Clone(
        string repository,
        IDirectoryInfo destinationPath,
        string @ref);

    Task<IEnumerable<(string Sha, string Ref)>> ListRemote(
        string repository,
        bool refs = false,
        bool tags = false);
}