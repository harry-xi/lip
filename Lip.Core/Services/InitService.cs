using Lip.Core.Context;
using Semver;

namespace Lip.Core.Services;

public class InitService
{
    private readonly IContext _context;
    private readonly IPathManager _pathManager;

    public InitService(IContext context)
    {
        _context = context;

        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);

        _pathManager = new PathManager(
            context.FileSystem,
            runtimeConfig.Cache,
            context.WorkingDir);
    }

    internal InitService(IContext context, IPathManager pathManager)
    {
        _context = context;
        _pathManager = pathManager;
    }

    private const string DefaultTooth = "example.com/org/package";
    private const string DefaultVersion = "0.1.0";

    public async Task Init()
    {
        string manifestPath = _pathManager.WorkspacePackageManifestPath;

        if (_context.FileSystem.File.Exists(manifestPath))
        {
            throw new InvalidOperationException($"The file '{manifestPath}' already exists.");
        }

        PackageManifest manifest = new()
        {
            ToothPath = DefaultTooth,
            Version = SemVersion.Parse(DefaultVersion),
        };

        using Stream stream = _context.FileSystem.File.OpenWrite(manifestPath);

        await PackageManifest.WriteToStreamAsync(manifest, stream);
    }
}