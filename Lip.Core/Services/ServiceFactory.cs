using Flurl;
using Lip.Core.Context;
using Lip.Core.PackageRegistries;

namespace Lip.Core.Services;

public static class ServiceFactory
{
    public static IPathManager CreatePathManager(IContext context, RuntimeConfig config)
    {
        return new PathManager(
            context.FileSystem,
            config.Cache,
            context.WorkingDir);
    }

    public static ICacheManager CreateCacheManager(IContext context, IPathManager pathManager, RuntimeConfig config)
    {
        return new CacheManager(
            context,
            pathManager,
            config.GitHubProxies.ConvertAll(Url.Parse),
            config.GoModuleProxies.ConvertAll(Url.Parse));
    }

    public static IWorkspaceManager CreateWorkspaceManager(IContext context, IPathManager pathManager, ICacheManager cacheManager)
    {
        return new WorkspaceManager(
            context.FileSystem,
            context.CommandRunner,
            context.Logger,
            context.UserInteraction,
            cacheManager,
            pathManager);
    }

    public static IPackageRegistry CreatePackageRegistry(IContext context, IPathManager pathManager, ICacheManager cacheManager, RuntimeConfig config)
    {
        return new CompositeRegistry(
        [
            new LiprRegistry(),
            .. config.GitHubProxies.Select(proxy => new GitRegistry(
                context.Git!,
                Url.Parse(proxy))),
            new GitRegistry(context.Git!),
            .. config.GoModuleProxies.Select(proxy => new GoProxyRegistry(
                cacheManager,
                pathManager,
                Url.Parse(proxy)))
        ]);
    }
}