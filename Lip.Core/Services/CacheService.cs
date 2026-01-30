using Lip.Core.PackageRegistries;
using System.Runtime.InteropServices;

namespace Lip.Core.Services;

public class CacheService
{
    private readonly IPackageRegistry _packageRegistry;
    private readonly ICacheManager _cacheManager;

    public CacheService(IContext context)
    {
        var pathManager = new PathManager(
            context.FileSystem,
            context.RuntimeConfig.Cache,
            context.WorkingDir);

        _cacheManager = new CacheManager(
            context,
            pathManager,
            context.RuntimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            context.RuntimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

        _packageRegistry = new PackageRegistry(
            context,
            _cacheManager,
            pathManager,
            context.RuntimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            context.RuntimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));
    }

    internal CacheService(IPackageRegistry packageRegistry, ICacheManager cacheManager)
    {
        _packageRegistry = packageRegistry;
        _cacheManager = cacheManager;
    }

    public record AddArgs { }
    public record CleanArgs { }
    public record ListArgs { }
    public record ListResult
    {
        public required List<string> DownloadedFiles { get; init; }
        public required List<string> GitRepos { get; init; }
    }

    public async Task Add(string packageSpecifierText, AddArgs _)
    {
        var packageSpecifier = PackageSpecifier.Parse(packageSpecifierText);

        PackageManifest packageManifest = await _packageRegistry.GetManifest(packageSpecifier)
            ?? throw new InvalidOperationException($"Cannot get package manifest from package '{packageSpecifier}'.");

        await _cacheManager.GetPackageFileSource(packageSpecifier);

        if (packageManifest.ToothPath != packageSpecifier.ToothPath)
        {
            throw new InvalidOperationException($"Tooth path in package manifest '{packageManifest.ToothPath}' does not match package specifier '{packageSpecifier.ToothPath}'.");
        }

        if (packageManifest.Version != packageSpecifier.Version)
        {
            throw new InvalidOperationException($"Version in package manifest '{packageManifest.Version}' does not match package specifier '{packageSpecifier.Version}'.");
        }

        PackageManifest.Variant? variant = packageManifest.GetVariant(
            packageSpecifier.VariantLabel, RuntimeInformation.RuntimeIdentifier);

        if (variant is null)
        {
            return;
        }

        foreach (PackageManifest.Asset asset in variant.Assets)
        {
            if (asset.Type != PackageManifest.Asset.TypeEnum.Self)
            {
                foreach (string url in asset.Urls)
                {
                    await _cacheManager.GetFileFromUrl(url);
                }
            }
        }
    }

    public async Task Clean(CleanArgs _)
    {
        await _cacheManager.Clean();
    }

    public async Task<ListResult> List(ListArgs _)
    {
        ICacheManager.ICacheSummary cacheSummary = await _cacheManager.List();
        return new ListResult
        {
            DownloadedFiles = [.. cacheSummary.DownloadedFiles.Keys],
            GitRepos = [.. cacheSummary.GitRepos.Keys.Select(repo => $"{repo.Url} {repo.Tag}")],
        };
    }
}