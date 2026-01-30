using Lip.Core.PackageRegistries;
using Scriban;
using Scriban.Parsing;
using System.Text;

namespace Lip.Core.Services;

public class ViewService
{
    private readonly IPackageRegistry _packageRegistry;

    public ViewService(IContext context)
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

        _packageRegistry = new PackageRegistry(
            context,
            cacheManager,
            pathManager,
            context.RuntimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            context.RuntimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));
    }

    internal ViewService(IPackageRegistry packageRegistry)
    {
        _packageRegistry = packageRegistry;
    }

    public record Args { }

    public async Task<string> View(string packageSpecifierText, string? path, Args args)
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
            return packageManifest.ToJsonElement().GetRawText();
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

        return template.Render(packageManifest.ToJsonElement());
    }
}