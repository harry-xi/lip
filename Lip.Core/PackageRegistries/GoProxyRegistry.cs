using Flurl;
using Flurl.Http;
using Golang.Org.X.Mod;
using Semver;

namespace Lip.Core.PackageRegistries;

public class GoProxyRegistry(
    ICacheManager cacheManager,
    IPathManager pathManager,
    Url goModuleProxy) : IPackageRegistry
{
    private readonly ICacheManager _cacheManager = cacheManager;
    private readonly Url _goModuleProxy = goModuleProxy;
    private readonly IPathManager _pathManager = pathManager;

    public async Task<PackageManifest> GetManifest(PackageSpecifier packageSpecifier)
    {
        IFileSource fileSource = await _cacheManager.GetPackageFileSource(packageSpecifier);

        using Stream? manifestStream = await fileSource.GetFileStream(_pathManager.PackageManifestFileName);

        if (manifestStream == null)
        {
            throw new InvalidOperationException("Package manifest not found in package.");
        }

        return await PackageManifest.FromStream(manifestStream);
    }

    public async Task<List<SemVersion>> GetVersions(PackageIdentifier packageIdentifier)
    {
        Url goModuleVersionListUrl = _goModuleProxy
            .Clone()
            .AppendPathSegments(
                Module.EscapePath(packageIdentifier.ToothPath).Item1,
                "@v",
                "list");


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
}