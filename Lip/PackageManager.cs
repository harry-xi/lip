using Lip.Context;

namespace Lip;

public class PackageManager(
    IContext context,
    PathManager pathManager
)
{
    private readonly IContext _context = context;
    private readonly PathManager _pathManager = pathManager;

    public async Task Install(IFileSource packageFileSource)
    {

    }

    public async Task<PackageLock> GetCurrentPackageLock()
    {
        string packageLockFilePath = _pathManager.CurrentPackageLockPath;

        // If the package lock file does not exist, return an empty package lock.
        if (!await _context.FileSystem.File.ExistsAsync(packageLockFilePath))
        {
            return new()
            {
                FormatVersion = PackageLock.DefaultFormatVersion,
                FormatUuid = PackageLock.DefaultFormatUuid,
                Locks = []
            };
        }

        byte[] packageLockBytes = await _context.FileSystem.File.ReadAllBytesAsync(packageLockFilePath);

        return PackageLock.FromJsonBytes(packageLockBytes);
    }

    public async Task<PackageManifest?> GetCurrentPackageManifest()
    {
        string packageManifestFilePath = _pathManager.CurrentPackageManifestPath;

        if (!await _context.FileSystem.File.ExistsAsync(packageManifestFilePath))
        {
            return null;
        }

        byte[] packageManifestBytes = await _context.FileSystem.File.ReadAllBytesAsync(packageManifestFilePath);

        return PackageManifest.FromJsonBytes(packageManifestBytes);
    }

    private async Task<bool> CheckInstalledPackage(PackageSpecifier packageSpecifier)
    {
        PackageLock packageLock = await GetCurrentPackageLock();

        return packageLock.Locks.Any(@lock => @lock.Package.ToothPath == packageSpecifier.ToothPath
                                              && @lock.Package.Version == packageSpecifier.Version
                                              && @lock.VariantLabel == packageSpecifier.VariantLabel);
    }
}
