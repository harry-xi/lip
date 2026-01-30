using Microsoft.Extensions.Logging;
using SharpCompress.Common;
using SharpCompress.Writers;
using System.Runtime.InteropServices;

namespace Lip.Core.Services;

public class PackService
{
    private readonly IContext _context;
    private readonly IPackageManager _packageManager;
    private readonly IPathManager _pathManager;

    public PackService(IContext context)
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

    internal PackService(IContext context, IPackageManager packageManager, IPathManager pathManager)
    {
        _context = context;
        _packageManager = packageManager;
        _pathManager = pathManager;
    }

    public record Args
    {
        public enum ArchiveFormatType
        {
            Zip,
            Tar,
            TarGz,
        }

        public bool DryRun { get; init; } = false;
        public bool IgnoreScripts { get; init; } = false;
        public ArchiveFormatType ArchiveFormat { get; init; } = ArchiveFormatType.Zip;
    }

    public async Task Pack(string outputPath, Args args)
    {
        PackageManifest packageManifest = await _packageManager.GetCurrentPackageManifest()
            ?? throw new InvalidOperationException("No package manifest found.");

        // Run pre-pack scripts.

        if (!args.IgnoreScripts)
        {
            var prePackScripts = packageManifest.GetVariant(
                string.Empty,
                RuntimeInformation.RuntimeIdentifier)?
                .Scripts
                .PrePack;

            if (prePackScripts != null)
            {
                foreach (var script in prePackScripts)
                {
                    _context.Logger.LogDebug("Running script: {script}", script);
                    if (!args.DryRun)
                    {
                        await _context.CommandRunner.Run(script, _pathManager.WorkingDir);
                    }
                }
            }
        }

        // Pack files in the archive.

        DirectoryFileSource fileSource = new(
            _context.FileSystem,
            _pathManager.WorkingDir);

        List<PackageManifest.Placement> filePlacements = [.. packageManifest.Variants
            .SelectMany(v => v.Assets)
            .Where(a => a.Type == PackageManifest.Asset.TypeEnum.Self)
            .SelectMany(a => a.Placements)];

        var allEntries = new List<IFileSourceEntry>();
        await foreach (var entry in fileSource.GetAllEntries())
        {
            allEntries.Add(entry);
        }
        List<IFileSourceEntry> fileEntriesToPlace = [.. allEntries
            .Where(entry => filePlacements.Any(placement => _pathManager.GetPlacementRelativePath(
                placement,
                entry.Key) is not null) || entry.Key == _pathManager.PackageManifestFileName)];

        using (Stream outputStream = !args.DryRun
            ? _context.FileSystem.File.Create(outputPath)
            : Stream.Null)
        using (IWriter writer = args.ArchiveFormat switch
        {
            Args.ArchiveFormatType.Zip => WriterFactory.Open(
                outputStream,
                ArchiveType.Zip,
                new(CompressionType.Deflate)),
            Args.ArchiveFormatType.Tar => WriterFactory.Open(
                outputStream,
                ArchiveType.Tar,
                new(CompressionType.None)),
            Args.ArchiveFormatType.TarGz => WriterFactory.Open(
                outputStream,
                ArchiveType.Tar,
                new(CompressionType.GZip)),
            _ => throw new NotImplementedException(),
        })
        {
            foreach (IFileSourceEntry entry in fileEntriesToPlace)
            {
                using Stream entryStream = await entry.OpenRead();

                writer.Write(entry.Key, await entry.OpenRead());
            }
        }

        foreach (var entry in allEntries)
        {
            await entry.DisposeAsync();
        }

        // Run post-pack scripts.

        if (!args.IgnoreScripts)
        {
            var postPackScripts = packageManifest.GetVariant(
                string.Empty,
                RuntimeInformation.RuntimeIdentifier)?
                .Scripts
                .PostPack;

            if (postPackScripts != null)
            {
                foreach (var script in postPackScripts)
                {
                    _context.Logger.LogDebug("Running script: {script}", script);
                    if (!args.DryRun)
                    {
                        await _context.CommandRunner.Run(script, _pathManager.WorkingDir);
                    }
                }
            }
        }
    }
}