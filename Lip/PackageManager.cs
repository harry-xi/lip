using DotNet.Globbing;
using Flurl;
using Lip.Context;
using Microsoft.Extensions.Logging;
using Semver;
using System.IO.Abstractions;
using System.Runtime.InteropServices;

namespace Lip;

public interface IPackageManager
{
    Task<PackageLock> GetCurrentPackageLock();
    Task<PackageManifest?> GetCurrentPackageManifest();
    Task<PackageLock.Package?> GetPackageFromLock(PackageIdentifier packageSpecifier);
    Task<PackageManifest?> GetPackageManifestFromCache(PackageSpecifier packageSpecifier);
    Task<PackageManifest?> GetPackageManifestFromFileSource(IFileSource fileSource);
    Task<List<SemVersion>> GetPackageRemoteVersions(PackageIdentifier packageSpecifier);
    Task InstallPackage(IFileSource packageFileSource, string variantLabel, bool dryRun, bool ignoreScripts, bool locked);
    Task SaveCurrentPackageManifest(PackageManifest packageManifest);
    Task UninstallPackage(PackageIdentifier packageSpecifierWithoutVersion, bool dryRun, bool ignoreScripts);
}

public class PackageManager(
    IContext context,
    ICacheManager cacheManager,
    IPathManager pathManager,
    List<Url> goModuleProxies) : IPackageManager
{
    private readonly ICacheManager _cacheManager = cacheManager;
    private readonly IContext _context = context;
    private readonly List<Url> _goModuleProxies = goModuleProxies;
    private readonly IPathManager _pathManager = pathManager;

    public async Task<PackageLock> GetCurrentPackageLock()
    {
        string packageLockFilePath = _pathManager.CurrentPackageLockPath;

        // If the package lock file does not exist, return an empty package lock.
        if (!_context.FileSystem.File.Exists(packageLockFilePath))
        {
            return new()
            {
                Packages = []
            };
        }

        using Stream packageLockFileStream = _context.FileSystem.File.OpenRead(packageLockFilePath);

        return await PackageLock.FromStream(packageLockFileStream);
    }

    public async Task<PackageManifest?> GetCurrentPackageManifest()
    {
        string packageManifestFilePath = _pathManager.CurrentPackageManifestPath;

        if (!_context.FileSystem.File.Exists(packageManifestFilePath))
        {
            return null;
        }

        using Stream fileStream = _context.FileSystem.File.OpenRead(packageManifestFilePath);

        return await PackageManifest.FromStream(fileStream);
    }

    public async Task<PackageLock.Package?> GetPackageFromLock(PackageIdentifier packageSpecifier)
    {
        PackageLock packageLock = await GetCurrentPackageLock();

        PackageLock.Package? package = packageLock.Packages.Where(
            @lock => @lock.Specifier.Identifier == packageSpecifier).FirstOrDefault();

        return package;
    }

    public async Task<PackageManifest?> GetPackageManifestFromCache(PackageSpecifier packageSpecifier)
    {
        IFileSource fileSource = await _cacheManager.GetPackageFileSource(packageSpecifier);

        return await GetPackageManifestFromFileSource(fileSource);
    }

    public async Task<PackageManifest?> GetPackageManifestFromFileSource(IFileSource fileSource)
    {
        using Stream? manifestStream = await fileSource.GetFileStream(_pathManager.PackageManifestFileName);

        if (manifestStream == null)
        {
            return null;
        }

        return await PackageManifest.FromStream(manifestStream);
    }

    public async Task<List<SemVersion>> GetPackageRemoteVersions(PackageIdentifier packageSpecifier)
    {
        // First, try to get remote versions from the Go module proxy.

        if (_goModuleProxies.Count != 0)
        {
            List<Url> goModuleVersionListUrls = _goModuleProxies.ConvertAll(proxy =>
                proxy.Clone()
                    .AppendPathSegments(
                        GoModule.EscapePath(packageSpecifier.ToothPath),
                        "@v",
                        "list")
            );

            foreach (Url url in _goModuleProxies)
            {
                Url goModuleVersionListUrl = url
                    .AppendPathSegments(
                        GoModule.EscapePath(packageSpecifier.ToothPath),
                        "@v",
                        "list");

                string tempFilePath = _context.FileSystem.Path.GetTempFileName();

                try
                {
                    await _context.Downloader.DownloadFile(goModuleVersionListUrl, tempFilePath);

                    string goModuleVersionListText = await _context.FileSystem.File.ReadAllTextAsync(tempFilePath);

                    return [.. goModuleVersionListText
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(s => s.StartsWith('v'))
                        .Select(versionText => versionText.Trim('v'))
                        .Select(versionText => SemVersion.TryParse(versionText, out SemVersion? version) ? version : null)
                        .Where(version => version is not null)];
                }
                catch (Exception ex)
                {
                    _context.Logger.LogWarning(ex, "Failed to download {Url}. Attempting next URL.", url);
                }
            }

            _context.Logger.LogWarning(
                "Failed to download version list for {Package} from all Go module proxies.",
                packageSpecifier);
        }

        // Second, try to get remote versions from the Git repository.

        if (_context.Git is not null)
        {
            string repoUrl = Url.Parse($"https://{packageSpecifier.ToothPath}");
            return [.. (await _context.Git.ListRemote(repoUrl, refs: true, tags: true))
                .Where(item => item.Ref.StartsWith("refs/tags/v"))
                .Select(item => item.Ref)
                .Select(refName => refName.Substring("refs/tags/v".Length))
                .Select(version => SemVersion.Parse(version))];
        }

        // Otherwise, no remote source is available.

        throw new InvalidOperationException("No remote source is available.");
    }

    public async Task InstallPackage(IFileSource packageFileSource, string variantLabel, bool dryRun, bool ignoreScripts, bool locked)
    {
        using Stream packageManifestFileStream = await packageFileSource.GetFileStream(_pathManager.PackageManifestFileName)
            ?? throw new InvalidOperationException("Package manifest not found.");

        PackageManifest packageManifest = await PackageManifest.FromStream(packageManifestFileStream);

        PackageSpecifier packageSpecifier = new()
        {
            ToothPath = packageManifest.ToothPath,
            Version = packageManifest.Version,
            VariantLabel = variantLabel
        };

        // If the package has already been installed, skip installing. Or if the package has been
        // installed with a different version, throw exception.

        SemVersion? installedVersion = (await GetPackageFromLock(packageSpecifier.Identifier))?.Specifier.Version;

        if (installedVersion == packageSpecifier.Version)
        {
            _context.Logger.LogInformation("Package {packageSpecifier} is already installed with version {installedVersion}", packageSpecifier, installedVersion);
            return;
        }

        if (installedVersion != null)
        {
            throw new InvalidOperationException($"Package {packageSpecifier} is already installed with version {installedVersion}.");
        }

        // If the package does not contain the variant to install, throw exception.

        PackageManifest.Variant packageVariant = packageManifest.GetVariant(
            variantLabel,
            RuntimeInformation.RuntimeIdentifier)
            ?? throw new InvalidOperationException($"The package does not contain variant {variantLabel}.");

        // Run pre-install scripts.

        if (!ignoreScripts)
        {
            packageVariant.Scripts.PreInstall.ForEach(script =>
            {
                _context.Logger.LogDebug("Running script: {script}", script);

                if (!dryRun)
                {
                    _context.CommandRunner.Run(
                        script,
                        _pathManager.WorkingDir);
                }
            });
        }

        // Place files.
        List<string> placedFiles = [];

        foreach (PackageManifest.Asset asset in packageVariant.Assets)
        {
            IFileSource fileSource = await GetAssetFileSource(asset, packageFileSource);

            List<IFileSourceEntry> entrys = await fileSource.GetAllEntries();

            foreach (IFileSourceEntry fileSourceEntry in await fileSource.GetAllEntries())
            {
                foreach (PackageManifest.Placement place in asset.Placements)
                {
                    string? destRelative = _pathManager.GetPlacementRelativePath(place, fileSourceEntry.Key);

                    if (destRelative is null)
                    {
                        continue;
                    }

                    string destPath = _context.FileSystem.Path.Join(_pathManager.WorkingDir, place.Dest, destRelative);

                    if (_context.FileSystem.Path.Exists(destPath))
                    {
                        throw new InvalidOperationException($"File {destPath} already exists.");
                    }

                    _context.Logger.LogDebug("Placing file: {entryKey} -> {destPath}", fileSourceEntry.Key, destPath);

                    if (!dryRun)
                    {
                        using Stream fileSourceEntryStream = await fileSourceEntry.OpenRead();

                        _context.FileSystem.CreateParentDirectory(destPath);

                        using Stream fileStream = _context.FileSystem.File.OpenWrite(destPath);

                        await fileSourceEntryStream.CopyToAsync(fileStream);

                        placedFiles.Add(_context.FileSystem.Path.Join(place.Dest, destRelative));
                    }
                }
            }
        }

        // Run install scripts.

        if (!ignoreScripts)
        {
            packageVariant.Scripts.Install.ForEach(script =>
            {
                _context.Logger.LogDebug("Running script: {script}", script);

                if (!dryRun)
                {
                    _context.CommandRunner.Run(
                        script,
                        _pathManager.WorkingDir);
                }
            });
        }

        // Update package lock.

        PackageLock packageLock = await GetCurrentPackageLock();

        packageLock.Packages.Add(new()
        {
            Manifest = packageManifest,
            VariantLabel = variantLabel,
            Locked = locked,
            Files = placedFiles,
        });

        await SaveCurrentPackageLock(packageLock);

        // Run post-install scripts.

        if (!ignoreScripts)
        {
            packageVariant.Scripts.PostInstall.ForEach(script =>
            {
                _context.Logger.LogDebug("Running script: {script}", script);
                if (!dryRun)
                {
                    _context.CommandRunner.Run(
                        script,
                        _pathManager.WorkingDir);
                }
            });
        }

        _context.Logger.LogInformation("Package {packageSpecifier} installed.", packageSpecifier);
    }

    public async Task SaveCurrentPackageManifest(PackageManifest packageManifest)
    {
        using Stream stream = _context.FileSystem.File.OpenWrite(_pathManager.CurrentPackageManifestPath);

        await packageManifest.ToStream(stream);
    }

    public async Task UninstallPackage(PackageIdentifier packageSpecifierWithoutVersion, bool dryRun, bool ignoreScripts)
    {
        PackageLock.Package? packageToUninstall = await GetPackageFromLock(packageSpecifierWithoutVersion);

        // If the package is not installed, skip uninstalling.

        if (packageToUninstall is null)
        {
            _context.Logger.LogWarning("Package {packageSpecifier} is not installed.", packageSpecifierWithoutVersion);
            return;
        }

        // Run pre-uninstall scripts.

        if (!ignoreScripts)
        {
            packageToUninstall.Variant.Scripts.PreUninstall.ForEach(script =>
            {
                _context.Logger.LogDebug("Running script: {script}", script);

                if (!dryRun)
                {
                    _context.CommandRunner.Run(
                        script,
                        _pathManager.WorkingDir);
                }
            });
        }

        // Run uninstall scripts.

        if (!ignoreScripts)
        {
            packageToUninstall.Variant.Scripts.Uninstall.ForEach(script =>
            {
                _context.Logger.LogDebug("Running script: {script}", script);

                if (!dryRun)
                {
                    _context.CommandRunner.Run(
                        script,
                        _pathManager.WorkingDir);
                }
            });
        }

        // Remove placed files.

        IEnumerable<Glob> preserveFileGlobs = packageToUninstall.Variant.PreserveFiles.Select(p => Glob.Parse(p));

        foreach (string file in packageToUninstall.Files)
        {
            if (preserveFileGlobs.Any(p => p.IsMatch(file)))
            {
                continue;
            }

            string destPath = _context.FileSystem.Path.Join(_pathManager.WorkingDir, file);

            _context.Logger.LogDebug("Removing file: {destPath}", destPath);

            if (!dryRun)
            {
                if (_context.FileSystem.File.Exists(destPath))
                {
                    _context.FileSystem.File.Delete(destPath);
                }

                RemoveParentDirectoriesUntilWorkingDir(destPath);
            }
        }

        // Remove files to remove.

        IEnumerable<Glob> removeFileGlobs = packageToUninstall.Variant.RemoveFiles.Select(p => Glob.Parse(p));

        foreach (string fullPath in _context.FileSystem.Directory.EnumerateFileSystemEntries(_pathManager.WorkingDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = _context.FileSystem.Path.GetRelativePath(_pathManager.WorkingDir, fullPath);

            if (!removeFileGlobs.Any(p => p.IsMatch(relativePath)))
            {
                continue;
            }

            string destPath = _context.FileSystem.Path.Join(_pathManager.WorkingDir, relativePath);

            _context.Logger.LogDebug("Removing file: {destPath}", destPath);

            if (!dryRun)
            {
                if (_context.FileSystem.File.Exists(destPath))
                {
                    _context.FileSystem.File.Delete(destPath);
                }

                RemoveParentDirectoriesUntilWorkingDir(destPath);
            }

        }

        // Update package lock.

        PackageLock packageLock = await GetCurrentPackageLock();

        packageLock.Packages.RemoveAll(@lock => @lock.Specifier == packageToUninstall.Specifier);

        await SaveCurrentPackageLock(packageLock);

        // Run post-uninstall scripts.

        if (!ignoreScripts)
        {
            packageToUninstall.Variant.Scripts.PostUninstall.ForEach(script =>
            {
                _context.Logger.LogDebug("Running script: {script}", script);

                if (!dryRun)
                {
                    _context.CommandRunner.Run(
                        script,
                        _pathManager.WorkingDir);
                }
            });
        }
    }

    private async Task<IFileSource> GetAssetFileSource(PackageManifest.Asset asset, IFileSource packageFileScore)
    {
        if (asset.Type == PackageManifest.Asset.TypeEnum.Self)
        {
            return packageFileScore;
        }

        IFileInfo assetFile = await _cacheManager.GetFileFromUrls(asset.Urls);

        if (asset.Type == PackageManifest.Asset.TypeEnum.Uncompressed)
        {
            return new StandaloneFileSource(_context.FileSystem, assetFile.FullName);
        }
        else
        {
            return new ArchiveFileSource(_context.FileSystem, assetFile.FullName);
        }
    }

    private void RemoveParentDirectoriesUntilWorkingDir(string path)
    {
        path = _context.FileSystem.Path.Combine(_pathManager.WorkingDir, path);

        string? parentDir = _context.FileSystem.Path.GetDirectoryName(path);

        while (parentDir != null
               && parentDir.StartsWith(_pathManager.WorkingDir)
               && parentDir != _pathManager.WorkingDir)
        {
            if (_context.FileSystem.Directory.Exists(parentDir)
                && !_context.FileSystem.Directory.EnumerateFileSystemEntries(parentDir).Any())
            {
                _context.FileSystem.Directory.Delete(parentDir);
            }

            parentDir = _context.FileSystem.Path.GetDirectoryName(parentDir);
        }
    }

    private async Task SaveCurrentPackageLock(PackageLock packageLock)
    {
        using Stream packageLockFileStream = _context.FileSystem.File.Create(_pathManager.CurrentPackageLockPath);

        await packageLock.ToStream(packageLockFileStream);
    }
}
