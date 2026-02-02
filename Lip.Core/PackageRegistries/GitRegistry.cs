using Flurl;
using Lip.Core.Context;
using Microsoft.Extensions.Logging;
using Semver;

namespace Lip.Core.PackageRegistries;

public class GitRegistry(
    IGit git,
    ILogger logger,
    List<Url> gitHubProxies) : IPackageRegistry
{
    private readonly IGit _git = git;
    private readonly ILogger _logger = logger;
    private readonly List<Url> _gitHubProxies = gitHubProxies;

    public async Task<PackageManifest> GetManifest(PackageSpecifier packageSpecifier)
    {
        throw new NotImplementedException();
    }

    public async Task<List<SemVersion>> GetVersions(PackageIdentifier packageIdentifier)
    {
        if (_git is null)
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
                    .. (await _git.ListRemote(url, refs: true, tags: true))
                        .Where(item => item.Ref.StartsWith("refs/tags/v"))
                        .Select(item => item.Ref)
                        .Select(refName => refName["refs/tags/v".Length..])
                        .Where(version => SemVersion.TryParse(version, out _))
                        .Select(version => SemVersion.Parse(version))
                ];
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Failed to clone {Url}. Attempting next URL.",
                    url);
                _logger.LogDebug(ex, "");
            }
        }

        throw new InvalidOperationException("Failed to get remote versions from all sources.");
    }
}