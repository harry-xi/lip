using Flurl;
using Lip.Core.Context;
using Semver;

namespace Lip.Core.PackageRegistries;

public class GitRegistry(IGit git, Url? gitHubProxy = null) : IPackageRegistry
{
    private readonly IGit _git = git;
    private readonly Url? _gitHubProxy = gitHubProxy;

    public async Task<PackageManifest> GetManifest(PackageSpecifier packageSpecifier)
    {
        throw new NotImplementedException();
    }

    public async Task<List<SemVersion>> GetVersions(PackageIdentifier packageIdentifier)
    {
        Url repoUrl = Url.Parse($"https://{packageIdentifier.ToothPath}");

        // Apply GitHub proxy to GitHub URLs.
        Url actualUrl = (_gitHubProxy is not null && repoUrl.Host == "github.com")
            ? _gitHubProxy
                .Clone()
                .AppendPathSegment(repoUrl.Path)
                .SetQueryParams(repoUrl.QueryParams)
            : repoUrl;

        return
        [
            .. (await _git.ListRemote(actualUrl, refs: true, tags: true))
                .Where(item => item.Ref.StartsWith("refs/tags/v"))
                .Select(item => item.Ref)
                .Select(refName => refName["refs/tags/v".Length..])
                .Where(version => SemVersion.TryParse(version, out _))
                .Select(version => SemVersion.Parse(version))
        ];
    }
}