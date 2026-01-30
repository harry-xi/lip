using System.Runtime.InteropServices;

namespace Lip.Core.Services;

public class RunService
{
    private readonly IContext _context;
    private readonly IPackageManager _packageManager;
    private readonly IPathManager _pathManager;

    public RunService(IContext context)
    {
        _context = context;

        _pathManager = new PathManager(
            context.FileSystem,
            context.RuntimeConfig.Cache,
            context.WorkingDir);

        var cacheManager = new CacheManager(
            context,
            _pathManager,
            context.RuntimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            context.RuntimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

        _packageManager = new PackageManager(context, cacheManager, _pathManager);
    }

    internal RunService(IContext context, IPackageManager packageManager, IPathManager pathManager)
    {
        _context = context;
        _packageManager = packageManager;
        _pathManager = pathManager;
    }

    public record Args
    {
        public string VariantLabel { get; init; } = string.Empty;
    }

    public async Task Run(string scriptName, Args args)
    {
        PackageManifest? packageManifest = await _packageManager.GetCurrentPackageManifest()
            ?? throw new InvalidOperationException("No package manifest found.");

        PackageManifest.Variant? variant = packageManifest.GetVariant(
            args.VariantLabel,
            RuntimeInformation.RuntimeIdentifier)
            ?? throw new InvalidOperationException($"Variant '{args.VariantLabel}' not found in package manifest");

        List<string> scripts = (variant.Scripts!.AdditionalScripts.GetValueOrDefault(scriptName))
            ?? throw new InvalidOperationException($"Script '{scriptName}' not found in package manifest with variant '{args.VariantLabel}'");

        foreach (string script in scripts)
        {
            await _context.CommandRunner.Run(script, _pathManager.WorkingDir);
        }
    }
}