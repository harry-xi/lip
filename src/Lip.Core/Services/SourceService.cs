using Flurl;
using Lip.Core.Entities;
using Lip.Core.SourceProviders;

namespace Lip.Core.Services;

public interface ISourceService
{
    enum ParsingMode
    {
        Composite,
        Single,
    }

    Task<ISourceProvider> Get(LocalPackageSpec localPackageSpec);
    Task<ISourceProvider> Get(PackageSpec packageSpec);
    Task<ISourceProvider> Get(RemotePackageSpec remotePackageSpec);

    /// <param name="url">
    /// Supported formats include:
    /// - Local directories or files with absolute paths: `file:///path/to/target`
    /// - Remote files: `https://example.com/path/to/target`
    /// - Git repositories: `git+https://example.com/path/to/repo.git#ref`
    /// - Go modules: `go://example.com/path/to/module#v1.0.0`
    /// </param>
    Task<ISourceProvider> Get(Url url, ParsingMode parsingMode);
}