using Lip.Core.Context;
using Lip.Core.PackageRegistries;
using System.Runtime.InteropServices;

namespace Lip.Core.Services;

public class CacheService
{
    private readonly IPackageRegistry _packageRegistry;
    private readonly ICacheManager _cacheManager;

    public CacheService(IContext context)
    {
        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);

        var pathManager = new PathManager(
            context.FileSystem,
            runtimeConfig.Cache,
            context.WorkingDir);

        _cacheManager = new CacheManager(
            context,
            pathManager,
            runtimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            runtimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

        _packageRegistry = new CompositeRegistry(
        [
            new LiprRegistry(),
            new GitRegistry(
                context.Git!,
                context.Logger,
                runtimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse)),
            new GoProxyRegistry(
                context.Logger,
                _cacheManager,
                pathManager,
                runtimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse)),
        ]);
    }

    internal CacheService(IPackageRegistry packageRegistry, ICacheManager cacheManager)
    {
        _packageRegistry = packageRegistry;
        _cacheManager = cacheManager;
    }



    public async Task Add(string packageSpecifierText)
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

    public async Task Clean()
    {
        await _cacheManager.Clean();
    }

    public async Task<(List<string> DownloadedFiles, List<string> GitRepos)> List()
    {
        var (downloadedFiles, gitRepos) = await _cacheManager.List();
        return (
            [.. downloadedFiles.Keys],
            [.. gitRepos.Keys.Select(repo => $"{repo.Url} {repo.Tag}")]
        );
    }
}