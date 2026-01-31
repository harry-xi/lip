using Lip.Core.Context;

namespace Lip.Core.Services;

public class ListService
{
    private readonly IPackageManager _packageManager;

    public ListService(IContext context)
    {
        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);

        var pathManager = new PathManager(
            context.FileSystem,
            runtimeConfig.Cache,
            context.WorkingDir);

        var cacheManager = new CacheManager(
            context,
            pathManager,
            runtimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            runtimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

        _packageManager = new PackageManager(context, cacheManager, pathManager);
    }

    internal ListService(IPackageManager packageManager)
    {
        _packageManager = packageManager;
    }



    public async Task<List<(bool Locked, PackageSpecifier Specifier, PackageManifest.Variant Variant)>> List()
    {
        PackageLock packageLock = await _packageManager.GetCurrentPackageLock();

        return packageLock.Packages
            .ConvertAll(@lock => (@lock.Locked, @lock.Specifier, @lock.Variant));
    }
}