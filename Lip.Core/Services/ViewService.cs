using Lip.Core.Context;
using Lip.Core.PackageRegistries;
using System.Text.Json;

namespace Lip.Core.Services;

public class ViewService
{
    private readonly IPackageRegistry _packageRegistry;

    public ViewService(IContext context)
    {
        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);
        var pathManager = ServiceFactory.CreatePathManager(context, runtimeConfig);
        var cacheManager = ServiceFactory.CreateCacheManager(context, pathManager, runtimeConfig);
        _packageRegistry = ServiceFactory.CreatePackageRegistry(context, pathManager, cacheManager, runtimeConfig);
    }

    internal ViewService(IPackageRegistry packageRegistry)
    {
        _packageRegistry = packageRegistry;
    }

    public async Task<string> View(string packageSpecifierText)
    {
        var packageSpecifier = PackageSpecifier.Parse(packageSpecifierText);

        PackageManifest packageManifest = await _packageRegistry.GetManifest(packageSpecifier);

        return JsonSerializer.Serialize(packageManifest, PackageManifest.JsonSerializerOptions);
    }
}