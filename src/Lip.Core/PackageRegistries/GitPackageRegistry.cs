using Flurl;
using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Semver;

namespace Lip.Core.PackageRegistries;

public class GitPackageRegistry(IGitRunner gitRunner, Url? githubProxy) : IPackageRegistry
{
    private readonly IGitRunner _gitRunner = gitRunner;

    private readonly Url? _githubProxy = githubProxy;

    public async Task<IEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId)
    {
        Url repoUrl = Url.Parse($"https://{packageId.Path}.git");

        if (_githubProxy is not null && repoUrl.Host == "github.com")
        {
            repoUrl = _githubProxy
                .Clone()
                .AppendPathSegments(repoUrl.PathSegments);
        }

        return (await _gitRunner.LsRemote(repoUrl, refs: true, tags: true))
            .Where(item => item.Ref.StartsWith("refs/tags/v"))
            .Select(item => item.Ref["refs/tags/v".Length..])
            .Where(version => SemVersion.TryParse(version, out _))
            .Select(version => SemVersion.Parse(version))
            .Order();
    }

    public Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec)
    {
        throw new NotSupportedException();
    }
}