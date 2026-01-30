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

    public record Args { }

    public record ResultItem
    {
        public required bool Locked { get; init; }
        public required PackageSpecifier Specifier { get; init; }
        public required PackageManifest.Variant Variant { get; init; }
    }

    public async Task<List<ResultItem>> List(Args args)
    {
        PackageLock packageLock = await _packageManager.GetCurrentPackageLock();

        List<ResultItem> listItems = packageLock.Packages
            .ConvertAll(@lock => new ResultItem
            {
                Locked = @lock.Locked,
                Specifier = @lock.Specifier,
                Variant = @lock.Variant
            });

        return listItems;
    }
}