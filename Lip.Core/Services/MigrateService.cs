namespace Lip.Core.Services;

public class MigrateService(IContext context, IPathManager pathManager)
{
    private readonly IContext _context = context;
    private readonly IPathManager _pathManager = pathManager;

    public record Args
    {
    }

    public async Task Migrate(string inputPath, string? outputPath, Args args)
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