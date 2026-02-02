using Flurl;
using Lip.Core.Context;
using Microsoft.Extensions.Logging;
using Semver;

namespace Lip.Core.PackageRegistries;

public class GitRegistry(
    IContext context,
    ICacheManager cacheManager,
    IPathManager pathManager,
    List<Url> gitHubProxies) : IPackageRegistry
{
    private readonly ICacheManager _cacheManager = cacheManager;
    private readonly IContext _context = context;
    private readonly List<Url> _gitHubProxies = gitHubProxies;
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
        if (_context.Git is null)
        {
            throw new InvalidOperationException("Git is not available.");
        }

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
                    .. (await _context.Git.ListRemote(url, refs: true, tags: true))
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

        throw new InvalidOperationException("Failed to get remote versions from all sources.");
    }
}