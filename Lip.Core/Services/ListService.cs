using Lip.Core.Context;


namespace Lip.Core.Services;

public class ListService
{
    private readonly IPackageManager _packageManager;

    public ListService(IContext context)
    {
        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);
        var pathManager = ServiceFactory.CreatePathManager(context, runtimeConfig);
        var cacheManager = ServiceFactory.CreateCacheManager(context, pathManager, runtimeConfig);
        _packageManager = ServiceFactory.CreatePackageManager(context, pathManager, cacheManager);
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