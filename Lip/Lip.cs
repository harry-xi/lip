using Flurl;
using Lip.Context;

namespace Lip;

/// <summary>
/// The main class of the Lip library.
/// </summary>
public partial class Lip
{
    private readonly CacheManager _cacheManager;
    private readonly IContext _context;
    private readonly DependencySolver _dependencySolver;
    private readonly PackageManager _packageManager;
    private readonly PathManager _pathManager;
    private readonly RuntimeConfig _runtimeConfig;

    public Lip(RuntimeConfig runtimeConfig, IContext context)
    {
        _context = context;
        _runtimeConfig = runtimeConfig;

        _pathManager = new PathManager(context.FileSystem, baseCacheDir: runtimeConfig.Cache, workingDir: context.WorkingDir);

        List<Url> gitHubProxies = runtimeConfig.GitHubProxies.ConvertAll(url => new Url(url));
        List<Url> goModuleProxies = runtimeConfig.GoModuleProxies.ConvertAll(url => new Url(url));

        _cacheManager = new CacheManager(_context, _pathManager, gitHubProxies, goModuleProxies);

        _packageManager = new PackageManager(_context, _cacheManager, _pathManager, goModuleProxies);

        _dependencySolver = new DependencySolver(_packageManager);
    }
}
