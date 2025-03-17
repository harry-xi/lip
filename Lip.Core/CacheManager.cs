using Flurl;
using Golang.Org.X.Mod;
using Microsoft.Extensions.Logging;
using Semver;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace Lip.Core;

public interface ICacheManager
{
    public interface ICacheSummary
    {
        Dictionary<Url, IFileInfo> DownloadedFiles { get; init; }
        Dictionary<IPathManager.IGitRepoInfo, IDirectoryInfo> GitRepos { get; init; }
    }

    Task Clean();
    Task<IFileInfo> GetFileFromUrl(Url url);
    Task<IFileInfo> GetFileFromUrls(List<Url> originalUrls);
    Task<IFileSource> GetPackageFileSource(PackageSpecifier packageSpecifier);
    Task<ICacheSummary> List();
}

public class CacheManager(
    IContext context,
    IPathManager pathManager,
    List<Url> gitHubProxies,
    List<Url> goModuleProxies) : ICacheManager
{
    private readonly IContext _context = context;
    private readonly List<Url> _githubProxies = gitHubProxies;
    private readonly List<Url> _goModuleProxies = goModuleProxies;
    private readonly IPathManager _pathManager = pathManager;

    public async Task Clean()
    {
        await Task.CompletedTask; // Suppress warning.

        string baseCacheDir = _pathManager.BaseCacheDir;

        if (_context.FileSystem.Directory.Exists(baseCacheDir))
        {
            _context.FileSystem.Directory.Delete(baseCacheDir, recursive: true);
        }
    }

    public async Task<IFileInfo> GetFileFromUrl(Url url) => await GetFileFromUrls([url]);

    public async Task<IFileInfo> GetFileFromUrls(List<Url> originalUrls)
    {
        // Apply GitHub proxy to GitHub URLs.
        List<Url> actualUrls =
        [
            .. originalUrls.SelectMany(url =>
            {
                // For typical URLs, just return the URL.
                if (url.Host == "github.com" && _githubProxies.Count != 0)
                {
                    return _githubProxies.Select(proxy => proxy
                        .Clone()
                        .AppendPathSegment(url.Path)
                        .SetQueryParams(url.QueryParams)
                    );
                }

                return [url];
            })
        ];

        return await GetFileDirectlyFromUrls(actualUrls);
    }

    public async Task<IFileSource> GetPackageFileSource(PackageSpecifier packageSpecifier)
    {
        // First, try to get the package from the Go module proxy.

        if (_goModuleProxies.Count != 0)
        {
            IFileInfo goModuleArchive = await GetGoModuleArchive(packageSpecifier);

            return new GoModuleArchiveFileSource(
                _context.FileSystem,
                goModuleArchive.FullName,
                packageSpecifier.ToothPath,
                packageSpecifier.Version);
        }

        // Next, try to get the package from the Git repository.

        if (_context.Git is not null)
        {
            IDirectoryInfo repoDir = await GetGitRepoDir(packageSpecifier);

            return new DirectoryFileSource(_context.FileSystem, repoDir.FullName);
        }

        throw new InvalidOperationException("No remote source is available.");
    }

    public async Task<ICacheManager.ICacheSummary> List()
    {
        await Task.CompletedTask; // Suppress warning.

        List<IFileInfo> downloadedFiles = [];

        string baseDownloadedFileCacheDir = _pathManager.BaseDownloadedFileCacheDir;

        if (_context.FileSystem.Directory.Exists(baseDownloadedFileCacheDir))
        {
            foreach (IFileInfo fileInfo in _context.FileSystem.DirectoryInfo.New(baseDownloadedFileCacheDir)
                         .EnumerateFiles())
            {
                downloadedFiles.Add(fileInfo);
            }
        }

        List<IDirectoryInfo> gitRepos = [];

        string baseGitRepoCacheDir = _pathManager.BaseGitRepoCacheDir;

        if (_context.FileSystem.Directory.Exists(baseGitRepoCacheDir))
        {
            foreach (IDirectoryInfo dirInfo in _context.FileSystem.DirectoryInfo.New(baseGitRepoCacheDir)
                         .EnumerateDirectories())
            {
                foreach (IDirectoryInfo subdirInfo in dirInfo.EnumerateDirectories())
                {
                    gitRepos.Add(subdirInfo);
                }
            }
        }

        return new CacheSummary(
            DownloadedFiles: downloadedFiles.ToDictionary(file =>
                _pathManager.ParseDownloadedFileCachePath(file.FullName)),
            GitRepos: gitRepos.ToDictionary(dir => _pathManager.ParseGitRepoDirCachePath(dir.FullName))
        );
    }

    private async Task<IFileInfo> GetFileDirectlyFromUrls(List<Url> actualUrls)
    {
        foreach (Url url in actualUrls)
        {
            string filePath = _pathManager.GetDownloadedFileCachePath(url);

            if (_context.FileSystem.File.Exists(filePath))
            {
                return _context.FileSystem.FileInfo.New(filePath);
            }
        }

        foreach (Url url in actualUrls)
        {
            string filePath = _pathManager.GetDownloadedFileCachePath(url);

            _context.Logger.LogDebug("Downloading {Url} to {FilePath}.", url, filePath);

            _context.FileSystem.CreateParentDirectory(filePath);

            try
            {
                await _context.Downloader.DownloadFile(url, filePath);

                return _context.FileSystem.FileInfo.New(filePath);
            }
            catch (Exception ex)
            {
                _context.Logger.LogWarning(ex, "Failed to download {Url}. Attempting next URL.", url);
            }
        }

        throw new InvalidOperationException("All download attempts failed.");
    }

    private async Task<IDirectoryInfo> GetGitRepoDir(PackageSpecifier packageSpecifier)
    {
        Url repoUrl = Url.Parse($"https://{packageSpecifier.ToothPath}");

        // Apply GitHub proxy to GitHub URLs.
        IEnumerable<Url> actualUrls = (repoUrl.Host == "github.com" && _githubProxies.Count != 0)
            ? _githubProxies.Select(proxy => proxy
                .Clone()
                .AppendPathSegment(repoUrl.Path)
                .SetQueryParams(repoUrl.QueryParams)
            )
            : [repoUrl];

        return await GetGitRepoDirDirectlyFromUrl(actualUrls, $"v{packageSpecifier.Version}");
    }

    private async Task<IDirectoryInfo> GetGitRepoDirDirectlyFromUrl(IEnumerable<Url> actualUrls, string tag)
    {
        foreach (Url url in actualUrls)
        {
            string repoDirPath = _pathManager.GetGitRepoDirCachePath(url, tag);

            if (_context.FileSystem.Directory.Exists(repoDirPath))
            {
                return _context.FileSystem.DirectoryInfo.New(repoDirPath);
            }
        }

        foreach (Url url in actualUrls)
        {
            string repoDirPath = _pathManager.GetGitRepoDirCachePath(url, tag);

            _context.Logger.LogDebug("Cloning {Url} to {RepoDirPath}.", url, repoDirPath);

            _context.FileSystem.CreateParentDirectory(repoDirPath);

            try
            {
                // Here we assume that git availability is checked before calling this method.
                await _context.Git!.Clone(
                    url,
                    repoDirPath,
                    branch: tag,
                    depth: 1
                );
                return _context.FileSystem.DirectoryInfo.New(repoDirPath);
            }
            catch (Exception ex)
            {
                _context.Logger.LogWarning(ex, "Failed to clone {Url}. Attempting next URL.", url);
            }
        }

        throw new InvalidOperationException("All clone attempts failed.");
    }

    private async Task<IFileInfo> GetGoModuleArchive(PackageSpecifier packageSpecifier)
    {
        SemVersion version = packageSpecifier.Version;

        // When major >= 2 and there's no go.mod, GoProxy will add +incompatible in version
        // Reference: https://stackoverflow.com/questions/57355929/what-does-incompatible-in-go-mod-mean-will-it-cause-harm
        if (version.Major >= 2)
        {
            version = version.WithMetadata("incompatible");
        }

        List<Url> archiveFileUrls = _goModuleProxies.ConvertAll(proxy =>
            proxy
                .Clone()
                .AppendPathSegments(
                    Module.EscapePath(packageSpecifier.ToothPath).Item1,
                    "@v",
                    Module.EscapeVersion(Module.CanonicalVersion("v" + version.ToString())).Item1 + ".zip"
                )
        );

        return await GetFileFromUrls(archiveFileUrls);
    }
}

[ExcludeFromCodeCoverage]
file record CacheSummary(
    Dictionary<Url, IFileInfo> DownloadedFiles,
    Dictionary<IPathManager.IGitRepoInfo, IDirectoryInfo> GitRepos
) : ICacheManager.ICacheSummary;
