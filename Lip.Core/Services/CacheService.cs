using Lip.Core.Context;

namespace Lip.Core.Services;

public class CacheService
{
    private readonly ICacheManager _cacheManager;

    public CacheService(IContext context)
    {
        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);
        var pathManager = ServiceFactory.CreatePathManager(context, runtimeConfig);
        _cacheManager = ServiceFactory.CreateCacheManager(context, pathManager, runtimeConfig);
    }

    internal CacheService(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }

    public async Task Clean()
    {
        await _cacheManager.Clean();
    }
}