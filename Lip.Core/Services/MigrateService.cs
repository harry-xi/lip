using Lip.Core.Context;

namespace Lip.Core.Services;

public class MigrateService
{
    private readonly IContext _context;
    private readonly IPathManager _pathManager;

    public MigrateService(IContext context)
    {
        _context = context;

        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);

        _pathManager = new PathManager(
            context.FileSystem,
            runtimeConfig.Cache,
            context.WorkingDir);
    }

    internal MigrateService(IContext context, IPathManager pathManager)
    {
        _context = context;
        _pathManager = pathManager;
    }



    public async Task Migrate(string inputPath, string? outputPath = null)
    {
        string realInputPath = _context.FileSystem.Path.Combine(
            _pathManager.WorkingDir,
            inputPath
        );

        PackageManifest packageManifest;
        using (var inputFileStream = _context.FileSystem.File.OpenRead(realInputPath))
        {
            packageManifest = await PackageManifest.FromStream(inputFileStream);
        }

        string realOutputPath = _context.FileSystem.Path.Combine(
            _pathManager.WorkingDir,
            outputPath ?? inputPath
        );

        using (var outputFileStream = _context.FileSystem.File.OpenWrite(realOutputPath))
        {
            await packageManifest.ToStream(outputFileStream);
        }
    }
}