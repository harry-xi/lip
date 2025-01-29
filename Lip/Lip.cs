using DotNet.Globbing;
using Flurl;
using Lip.Context;

namespace Lip;

/// <summary>
/// The main class of the Lip library.
/// </summary>
public partial class Lip
{
    private readonly CacheManager _cacheManager;
    private readonly IContext _context;
    private readonly PathManager _pathManager;
    private readonly RuntimeConfig _runtimeConfig;

    public Lip(RuntimeConfig runtimeConfig, IContext context)
    {
        _context = context;
        _runtimeConfig = runtimeConfig;

        _pathManager = new(context.FileSystem, baseCacheDir: runtimeConfig.Cache, workingDir: context.WorkingDir);

        Url? githubProxyUrl = runtimeConfig.GitHubProxy != string.Empty ? Url.Parse(runtimeConfig.GitHubProxy) : null;
        Url? goModuleProxyUrl = runtimeConfig.GoModuleProxy != string.Empty ? Url.Parse(runtimeConfig.GoModuleProxy) : null;
        _cacheManager = new(context, _pathManager, githubProxyUrl, goModuleProxyUrl);
    }

    private async Task<PackageLock> GetCurrentPackageLock()
    {
        string packageLockFilePath = _pathManager.CurrentPackageLockPath;

        // If the package lock file does not exist, return an empty package lock.
        if (!await _context.FileSystem.File.ExistsAsync(packageLockFilePath))
        {
            return new()
            {
                FormatVersion = PackageLock.DefaultFormatVersion,
                FormatUuid = PackageLock.DefaultFormatUuid,
                Packages = [],
                Locks = []
            };
        }

        byte[] packageLockBytes = await _context.FileSystem.File.ReadAllBytesAsync(packageLockFilePath);

        return PackageLock.FromJsonBytes(packageLockBytes);
    }

    private async Task<PackageManifest?> GetCurrentPackageManifest()
    {
        string packageManifestFilePath = _pathManager.CurrentPackageManifestPath;

        if (!await _context.FileSystem.File.ExistsAsync(packageManifestFilePath))
        {
            return null;
        }

        byte[] packageManifestBytes = await _context.FileSystem.File.ReadAllBytesAsync(packageManifestFilePath);

        return PackageManifest.FromJsonBytes(packageManifestBytes);
    }

    private string? GetPlacementRelativePath(PackageManifest.PlaceType placement, string fileSourceEntryKey)
    {
        if (placement.Type == PackageManifest.PlaceType.TypeEnum.File)
        {
            string fileName = _context.FileSystem.Path.GetFileName(fileSourceEntryKey);

            if (fileSourceEntryKey == placement.Src)
            {
                return fileName;
            }

            Glob glob = Glob.Parse(placement.Src);

            if (glob.IsMatch(fileSourceEntryKey))
            {
                return fileName;
            }

            return null;
        }
        else if (placement.Type == PackageManifest.PlaceType.TypeEnum.Dir)
        {
            string placementSrc = placement.Src;

            if (!placementSrc.EndsWith('/'))
            {
                placementSrc += '/';
            }

            if (!fileSourceEntryKey.StartsWith(placementSrc))
            {
                return null;
            }

            return fileSourceEntryKey[placementSrc.Length..];
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
