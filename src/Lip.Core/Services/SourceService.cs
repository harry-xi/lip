using Flurl;
using Flurl.Http;
using Golang.Org.X.Mod;
using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.SourceProviders;
using Semver;
using System.IO.Abstractions;

namespace Lip.Core.Services;

public interface ISourceService
{
    Task<ISourceProvider> Get(LocalPackageSpec localPackageSpec);
    Task<ISourceProvider> Get(PackageSpec packageSpec);
    Task<ISourceProvider> Get(RemotePackageSpec remotePackageSpec);
    Task<ISourceProvider> Get(Url url, bool isArchive);
}

public class SourceService(
    IGitRunner gitRunner,
    IUserInteraction userInteraction,
    ICacheService cacheService,
    Url? githubProxy,
    Url goModuleProxy) : ISourceService
{
    private readonly IGitRunner _gitRunner = gitRunner;
    private readonly IUserInteraction _userInteraction = userInteraction;

    private readonly ICacheService _cacheService = cacheService;

    private readonly Url? _githubProxy = githubProxy;
    private readonly Url _goModuleProxy = goModuleProxy;

    public async Task<ISourceProvider> Get(LocalPackageSpec localPackageSpec)
    {
        if (!localPackageSpec.ArchiveFile.Exists)
        {
            throw new FileNotFoundException($"The specified local package file does not exist: {localPackageSpec.ArchiveFile.FullName}");
        }

        return new ArchiveSourceProvider(localPackageSpec.ArchiveFile);
    }

    public async Task<ISourceProvider> Get(PackageSpec packageSpec)
    {
        List<Exception> exceptions = [];

        foreach (Func<Task<ISourceProvider>> sourceFunc in new Func<Task<ISourceProvider>>[]
        {
            () => GetPackageViaGoModuleProxy(packageSpec),
            () => GetPackageViaGit(packageSpec),
        })
        {
            try
            {
                return await sourceFunc();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException($"Failed to retrieve package '{packageSpec}' from all sources.", exceptions);
    }

    public async Task<ISourceProvider> Get(RemotePackageSpec remotePackageSpec)
    {
        return await Get(remotePackageSpec.ArchiveUrl, isArchive: true);
    }

    public async Task<ISourceProvider> Get(Url url, bool isArchive)
    {
        IFileInfo archiveFile = await _cacheService.GetOrCreateFile(url, async cacheFile =>
        {
            using Stream respStream = await url.GetStreamAsync();
            using Stream fileStream = cacheFile.OpenWrite();

            await _userInteraction.PrintInfo($"Downloading from '{url}'...");

            await respStream.CopyToAsync(fileStream);
        });

        return isArchive
            ? new ArchiveSourceProvider(archiveFile)
            : new SingleFileSourceProvider(archiveFile);
    }

    private async Task<ISourceProvider> GetPackageViaGit(PackageSpec packageSpec)
    {
        Url repoUrl = Url.Parse($"https://{packageSpec.Id.Path}.git");

        if (_githubProxy is not null && repoUrl.Host == "github.com")
        {
            repoUrl = _githubProxy
                .Clone()
                .AppendPathSegments(repoUrl.PathSegments);
        }

        string @ref = $"v{packageSpec.Version}";

        Url keyUrl = repoUrl.Clone();
        keyUrl.Scheme = $"git+{keyUrl.Scheme}";
        keyUrl.Fragment = @ref;

        IDirectoryInfo repoDir = await _cacheService.GetOrCreateDirectory(keyUrl, async cacheDir =>
        {
            await _userInteraction.PrintInfo($"Cloning git repository from '{repoUrl}'...");

            await _gitRunner.Clone(repoUrl, cacheDir.FullName, branch: @ref);
        });

        return new DirectorySourceProvider(repoDir);
    }

    private async Task<ISourceProvider> GetPackageViaGoModuleProxy(PackageSpec packageSpec)
    {
        SemVersion version = (packageSpec.Version.Major >= 2)
            ? packageSpec.Version.WithMetadata("incompatible")
            : packageSpec.Version;

        Url archiveUrl = _goModuleProxy
            .Clone()
            .AppendPathSegments(
            Module.EscapePath(packageSpec.Id.Path).Item1,
            "@v",
            Module.EscapeVersion(Module.CanonicalVersion($"v{version}")).Item1 + ".zip");

        IFileInfo archiveFile = await _cacheService.GetOrCreateFile(archiveUrl, async cacheFile =>
        {
            using Stream respStream = await archiveUrl.GetStreamAsync();
            using Stream fileStream = cacheFile.OpenWrite();

            await _userInteraction.PrintInfo($"Downloading from '{archiveUrl}'...");

            await respStream.CopyToAsync(fileStream);
        });

        return new GoModuleArchiveSourceProvider(archiveFile);
    }
}