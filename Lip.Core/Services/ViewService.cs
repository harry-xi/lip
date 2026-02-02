using Lip.Core.Context;
using Lip.Core.PackageRegistries;
using Scriban;
using Scriban.Parsing;
using System.Text;
using System.Text.Json;

namespace Lip.Core.Services;

public class ViewService
{
    private readonly IPackageRegistry _packageRegistry;

    public ViewService(IContext context)
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

        _packageRegistry = new CompositeRegistry(
        [
            new GoProxyRegistry(
                context,
                cacheManager,
                pathManager,
                runtimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse)),
            new GitRegistry(
                context,
                runtimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse))
        ]);
    }

    internal ViewService(IPackageRegistry packageRegistry)
    {
        _packageRegistry = packageRegistry;
    }



    public async Task<string> View(string packageSpecifierText, string? path = null)
    {
        var packageSpecifier = PackageSpecifier.Parse(packageSpecifierText);

        PackageManifest packageManifest = await _packageRegistry.GetManifest(packageSpecifier)
            ?? throw new InvalidOperationException($"Cannot get package manifest from package '{packageSpecifier}'.");

        if (packageManifest.ToothPath != packageSpecifier.ToothPath)
        {
            throw new InvalidOperationException($"Tooth path in package manifest '{packageManifest.ToothPath}' does not match package specifier '{packageSpecifier.ToothPath}'.");
        }

        if (packageManifest.Version != packageSpecifier.Version)
        {
            throw new InvalidOperationException($"Version in package manifest '{packageManifest.Version}' does not match package specifier '{packageSpecifier.Version}'.");
        }

        if (path is null)
        {
            return JsonSerializer.Serialize(packageManifest, PackageManifest.JsonSerializerOptions);
        }

        Template template = Template.Parse(path, lexerOptions: new()
        {
            Mode = ScriptMode.ScriptOnly
        });

        if (template.HasErrors)
        {
            StringBuilder sb = new();
            foreach (LogMessage message in template.Messages)
            {
                sb.Append(message.ToString());
            }
            throw new FormatException($"Failed to parse template '{path}': {sb}");
        }

        return template.Render(JsonSerializer.SerializeToElement(packageManifest, PackageManifest.JsonSerializerOptions));
    }
}