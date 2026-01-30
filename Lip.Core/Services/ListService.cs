namespace Lip.Core.Services;

public class ListService
{
    private readonly IPackageManager _packageManager;

    public ListService(IContext context)
    {
        var pathManager = new PathManager(
            context.FileSystem,
            context.RuntimeConfig.Cache,
            context.WorkingDir);

        var cacheManager = new CacheManager(
            context,
            pathManager,
            context.RuntimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            context.RuntimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

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