using Lip.Core.Context;


namespace Lip.Core.Services;

public class MigrateService(IContext context)
{
    private readonly IContext _context = context;



    public async Task Migrate(string inputPath, string outputPath)
    {
        PackageManifest packageManifest;
        using (var inputFileStream = _context.FileSystem.File.OpenRead(inputPath))
        {
            packageManifest = await PackageManifest.FromStream(inputFileStream);
        }

        using var outputFileStream = _context.FileSystem.File.OpenWrite(outputPath);
        await PackageManifest.WriteToStreamAsync(packageManifest, outputFileStream);
    }
}