using Flurl;
using Flurl.Http;
using Golang.Org.X.Mod;
using Lip.Core.Context;
using Microsoft.Extensions.Logging;
using Semver;

namespace Lip.Core.PackageRegistries;

public class PackageRegistry(
    IContext context,
    ICacheManager cacheManager,
    IPathManager pathManager,
    List<Url> gitHubProxies,
    List<Url> goModuleProxies) : IPackageRegistry
{
    private readonly ICacheManager _cacheManager = cacheManager;
    private readonly IContext _context = context;
    private readonly List<Url> _gitHubProxies = gitHubProxies;
    private readonly List<Url> _goModuleProxies = goModuleProxies;
    private readonly IPathManager _pathManager = pathManager;

    public async Task<PackageManifest?> GetManifest(PackageSpecifier packageSpecifier)
    {
        IFileSource fileSource = await _cacheManager.GetPackageFileSource(packageSpecifier);

        using Stream? manifestStream = await fileSource.GetFileStream(_pathManager.PackageManifestFileName);

        if (manifestStream == null)
        {
            return null;
        }

        return await PackageManifest.FromStream(manifestStream);
    }

    public async Task<List<SemVersion>> GetVersions(PackageIdentifier packageIdentifier)
    {
        // First, try to get remote versions from the Go module proxy.

        if (_goModuleProxies.Count != 0)
        {
            foreach (Url goModuleProxyUrl in _goModuleProxies)
            {
                Url goModuleVersionListUrl = goModuleProxyUrl
                    .Clone()
                    .AppendPathSegments(
                        Module.EscapePath(packageIdentifier.ToothPath).Item1,
                        "@v",
                        "list");

                try
                {
                    string goModuleVersionListText = await goModuleVersionListUrl.GetStringAsync();

                    return
                    [
                        .. goModuleVersionListText
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Where(s => s.StartsWith('v'))
                            .Select(versionText =>
                                SemVersion.TryParse(Golang.Org.X.Mod.Semver.Canonical(versionText).Trim('v'),
                                    out SemVersion? version)
                                    ? version
                                    : null)
                            .Where(version => version is not null)
                            .Select(version => version!)
                    ];
                }
                catch (Exception ex)
                {
                    _context.Logger.LogWarning("Failed to download {Url}. Attempting next URL.",
                        goModuleVersionListUrl);
                    _context.Logger.LogDebug(ex, "");
                }
            }

            _context.Logger.LogWarning(
                "Failed to download version list for {Package} from all Go module proxies.",
                packageIdentifier);
        }

        // Second, try to get remote versions from the Git repository.

        if (_context.Git is not null)
        {
            Url repoUrl = Url.Parse($"https://{packageIdentifier.ToothPath}");

            // Apply GitHub proxy to GitHub URLs.
            IEnumerable<Url> actualUrls = (repoUrl.Host == "github.com" && _gitHubProxies.Count != 0)
                ? _gitHubProxies.Select(proxy => proxy
                    .Clone()
                    .AppendPathSegment(repoUrl.Path)
                    .SetQueryParams(repoUrl.QueryParams)
                )
                : [repoUrl];

            foreach (Url url in actualUrls)
            {
                try
                {
                    return
                    [
                        .. (await _context.Git.ListRemote(repoUrl, refs: true, tags: true))
                            .Where(item => item.Ref.StartsWith("refs/tags/v"))
                            .Select(item => item.Ref)
                            .Select(refName => refName["refs/tags/v".Length..])
                            .Where(version => SemVersion.TryParse(version, out _))
                            .Select(version => SemVersion.Parse(version))
                    ];
                }
                catch (Exception ex)
                {
                    _context.Logger.LogWarning(
                        "Failed to clone {Url}. Attempting next URL.",
                        url);
                    _context.Logger.LogDebug(ex, "");
                }
            }
        }

        // Otherwise, no remote source is available.
        throw new InvalidOperationException("Failed to get remote versions from all sources.");
    }
}