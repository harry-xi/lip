using Lip.Core.Context;


namespace Lip.Core.Services;

public class ListService
{
    private readonly IWorkspaceManager _workspaceManager;

    public ListService(IContext context)
    {
        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);
        var pathManager = ServiceFactory.CreatePathManager(context, runtimeConfig);
        var cacheManager = ServiceFactory.CreateCacheManager(context, pathManager, runtimeConfig);
        _workspaceManager = ServiceFactory.CreateWorkspaceManager(context, pathManager, cacheManager);
    }

    internal ListService(IWorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }



    public async Task<List<PackageSpecifier>> List()
    {
        PackageLock packageLock = await _workspaceManager.GetCurrentPackageLock();

        return packageLock.Packages.ConvertAll(@lock => @lock.Specifier);
    }
}