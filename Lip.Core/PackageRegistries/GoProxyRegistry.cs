using Flurl;
using Flurl.Http;
using Golang.Org.X.Mod;
using Lip.Core.Context;
using Microsoft.Extensions.Logging;
using Semver;

namespace Lip.Core.PackageRegistries;

public class GoProxyRegistry(
    IContext context,
    ICacheManager cacheManager,
    IPathManager pathManager,
    List<Url> goModuleProxies) : IPackageRegistry
{
    private readonly ICacheManager _cacheManager = cacheManager;
    private readonly IContext _context = context;
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
        if (_goModuleProxies.Count == 0)
        {
            throw new InvalidOperationException("No Go module proxies configured.");
        }

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

        throw new InvalidOperationException($"Failed to download version list for {packageIdentifier} from all Go module proxies.");
    }
}