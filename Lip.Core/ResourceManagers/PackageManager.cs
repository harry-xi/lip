using DotNet.Globbing;
using Lip.Core.Context;
using Microsoft.Extensions.Logging;
using Semver;
using System.IO.Abstractions;
using System.Runtime.InteropServices;

namespace Lip.Core;

public interface IPackageManager
{
    Task<PackageLock> GetCurrentPackageLock();
    Task<PackageLock.Package?> GetPackageFromLock(PackageIdentifier packageSpecifier);
    Task<PackageManifest?> GetPackageManifestFromFileSource(IFileSource fileSource);

    Task InstallPackage(IFileSource packageFileSource, string variantLabel, bool dryRun, bool ignoreScripts,
        bool locked, bool overwriteFile);

    Task SaveCurrentPackageManifest(PackageManifest packageManifest);
    Task UninstallPackage(PackageIdentifier packageSpecifierWithoutVersion, bool dryRun, bool ignoreScripts);
}

public class PackageManager(
    IFileSystem fileSystem,
    ICommandRunner commandRunner,
    ILogger logger,
    IUserInteraction userInteraction,
    ICacheManager cacheManager,
    IPathManager pathManager) : IPackageManager
{
    private readonly ICacheManager _cacheManager = cacheManager;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ICommandRunner _commandRunner = commandRunner;
    private readonly ILogger _logger = logger;
    private readonly IUserInteraction _userInteraction = userInteraction;
    private readonly IPathManager _pathManager = pathManager;

    public async Task<PackageLock> GetCurrentPackageLock()
    {
        string packageLockFilePath = _pathManager.CurrentPackageLockPath;

        // If the package lock file does not exist, return an empty package lock.
        if (!_fileSystem.File.Exists(packageLockFilePath))
        {
            return new() { Packages = [] };
        }

        using Stream packageLockFileStream = _fileSystem.File.OpenRead(packageLockFilePath);

        return await PackageLock.FromStream(packageLockFileStream);
    }

    public async Task<PackageLock.Package?> GetPackageFromLock(PackageIdentifier packageSpecifier)
    {
        PackageLock packageLock = await GetCurrentPackageLock();

        PackageLock.Package? package = packageLock.Packages.FirstOrDefault(
            @lock => @lock.Specifier.Identifier == packageSpecifier);

        return package;
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

    public async Task InstallPackage(IFileSource packageFileSource, string variantLabel, bool dryRun,
        bool ignoreScripts, bool locked, bool overwriteFile)
    {
        using Stream packageManifestFileStream =
            await packageFileSource.GetFileStream(_pathManager.PackageManifestFileName)
            ?? throw new InvalidOperationException("Package manifest not found.");

        PackageManifest packageManifest = await PackageManifest.FromStream(packageManifestFileStream);

        PackageSpecifier packageSpecifier = new(new PackageIdentifier(packageManifest.ToothPath, variantLabel), packageManifest.Version);

        _logger.LogDebug("Installing package {packageSpecifier}...", packageSpecifier);

        // If the package has already been installed, skip installing. Or if the package has been
        // installed with a different version, throw exception.

        SemVersion? installedVersion = (await GetPackageFromLock(packageSpecifier.Identifier))?.Specifier.Version;

        if (installedVersion == packageSpecifier.Version)
        {
            _logger.LogInformation(
                "Package {packageSpecifier} is already installed with version {installedVersion}", packageSpecifier,
                installedVersion);
            return;
        }

        if (installedVersion != null)
        {
            throw new InvalidOperationException(
                $"Package {packageSpecifier} is already installed with version {installedVersion}.");
        }

        // If the package does not contain the variant to install, throw exception.

        PackageManifest.Variant packageVariant = packageManifest.GetVariant(
                                                     variantLabel,
                                                     RuntimeInformation.RuntimeIdentifier)
                                                 ?? throw new InvalidOperationException(
                                                     $"The package does not contain variant {variantLabel}.");

        // Run pre-install scripts.

        if (!ignoreScripts)
        {
            foreach (var script in packageVariant.Scripts.PreInstall)
            {
                _logger.LogDebug("Running script: {script}", script);
                if (!dryRun)
                {
                    await _commandRunner.Run(script, _pathManager.WorkingDir);
                }
            }
        }

        // Place files.
        List<string> placedFiles = [];

        foreach (PackageManifest.Asset asset in packageVariant.Assets)
        {
            IFileSource fileSource = await GetAssetFileSource(asset, packageFileSource);

            IAsyncEnumerable<IFileSourceEntry> entrys = fileSource.GetAllEntries();

            await foreach (IFileSourceEntry fileSourceEntry in fileSource.GetAllEntries())
            {
                foreach (PackageManifest.Placement place in asset.Placements)
                {
                    string? destRelative = _pathManager.GetPlacementRelativePath(place, fileSourceEntry.Key);

                    if (destRelative is null)
                    {
                        continue;
                    }

                    string destPath = _fileSystem.Path.Join(_pathManager.WorkingDir, place.Dest, destRelative);

                    if (!overwriteFile && _fileSystem.Path.Exists(destPath))
                    {
                        var select = await _userInteraction.PromptForSelection(["Yes", "No", "All"], $"File {destPath} already exists. Overwrite It?");
                        if (select == "No")
                        {
                            continue;
                        }
                        if (select == "All")
                        {
                            overwriteFile = true;
                        }
                    }

                    _logger.LogDebug("Placing file: {entryKey} -> {destPath}", fileSourceEntry.Key, destPath);

                    if (!dryRun)
                    {
                        using Stream fileSourceEntryStream = await fileSourceEntry.OpenRead();

                        _fileSystem.CreateParentDirectory(destPath);

                        using Stream fileStream = _fileSystem.File.OpenWrite(destPath);

                        await fileSourceEntryStream.CopyToAsync(fileStream);

                        placedFiles.Add(_fileSystem.Path.Join(place.Dest, destRelative));
                    }
                }

                await fileSourceEntry.DisposeAsync();
            }
        }

        // Run install scripts.

        if (!ignoreScripts)
        {
            foreach (var script in packageVariant.Scripts.Install)
            {
                _logger.LogDebug("Running script: {script}", script);
                if (!dryRun)
                {
                    await _commandRunner.Run(script, _pathManager.WorkingDir);
                }
            }
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

        if (!dryRun)
        {
            await SaveCurrentPackageLock(packageLock);
        }

        // Run post-install scripts.

        if (!ignoreScripts)
        {
            foreach (var script in packageVariant.Scripts.PostInstall)
            {
                _logger.LogDebug("Running script: {script}", script);
                if (!dryRun)
                {
                    await _commandRunner.Run(script, _pathManager.WorkingDir);
                }
            }
        }

        _logger.LogInformation("Package {packageSpecifier} installed.", packageSpecifier);
    }

    public async Task SaveCurrentPackageManifest(PackageManifest packageManifest)
    {
        using Stream stream = _fileSystem.File.OpenWrite(_pathManager.CurrentPackageManifestPath);

        await PackageManifest.WriteToStreamAsync(packageManifest, stream);
    }

    public async Task UninstallPackage(PackageIdentifier packageSpecifierWithoutVersion, bool dryRun,
        bool ignoreScripts)
    {
        PackageLock.Package? packageToUninstall = await GetPackageFromLock(packageSpecifierWithoutVersion);

        // If the package is not installed, skip uninstalling.

        if (packageToUninstall is null)
        {
            _logger.LogWarning("Package {packageSpecifier} is not installed.", packageSpecifierWithoutVersion);
            return;
        }

        _logger.LogDebug("Uninstalling package {packageSpecifier}...", packageToUninstall.Specifier);

        // Run pre-uninstall scripts.

        if (!ignoreScripts)
        {
            foreach (var script in packageToUninstall.Variant.Scripts.PreUninstall)
            {
                _logger.LogDebug("Running script: {script}", script);
                if (!dryRun)
                {
                    await _commandRunner.Run(script, _pathManager.WorkingDir);
                }
            }
        }

        // Run uninstall scripts.

        if (!ignoreScripts)
        {
            foreach (var script in packageToUninstall.Variant.Scripts.Uninstall)
            {
                _logger.LogDebug("Running script: {script}", script);
                if (!dryRun)
                {
                    await _commandRunner.Run(script, _pathManager.WorkingDir);
                }
            }
        }

        // Remove placed files.

        IEnumerable<Glob> preserveFileGlobs = packageToUninstall.Variant.PreserveFiles.Select(p => Glob.Parse(p));

        foreach (string file in packageToUninstall.Files)
        {
            if (preserveFileGlobs.Any(p => p.IsMatch(file)))
            {
                continue;
            }

            string destPath = _fileSystem.Path.Join(_pathManager.WorkingDir, file);

            _logger.LogDebug("Removing file: {destPath}", destPath);

            if (!dryRun)
            {
                if (_fileSystem.File.Exists(destPath))
                {
                    _fileSystem.File.Delete(destPath);
                }
                else if (_fileSystem.Directory.Exists(destPath))
                {
                    _fileSystem.Directory.Delete(destPath, true);
                }

                RemoveParentDirectoriesUntilWorkingDir(destPath);
            }
        }

        // Remove files to remove.

        IEnumerable<Glob> removeFileGlobs = packageToUninstall.Variant.RemoveFiles.Select(p => Glob.Parse(p));

        foreach (string fullPath in _fileSystem.Directory.EnumerateFileSystemEntries(_pathManager.WorkingDir,
                     "*", SearchOption.AllDirectories))
        {
            string relativePath = _fileSystem.Path.GetRelativePath(_pathManager.WorkingDir, fullPath);

            if (!removeFileGlobs.Any(p => p.IsMatch(relativePath)))
            {
                continue;
            }

            string destPath = _fileSystem.Path.Join(_pathManager.WorkingDir, relativePath);

            _logger.LogDebug("Removing file: {destPath}", destPath);

            if (!dryRun)
            {
                if (_fileSystem.File.Exists(destPath))
                {
                    _fileSystem.File.Delete(destPath);
                }
                else if (_fileSystem.Directory.Exists(destPath))
                {
                    _fileSystem.Directory.Delete(destPath, true);
                }

                RemoveParentDirectoriesUntilWorkingDir(destPath);
            }
        }

        // Update package lock.

        PackageLock packageLock = await GetCurrentPackageLock();

        packageLock.Packages.RemoveAll(@lock => @lock.Specifier == packageToUninstall.Specifier);

        if (!dryRun)
        {
            await SaveCurrentPackageLock(packageLock);
        }

        // Run post-uninstall scripts.

        if (!ignoreScripts)
        {
            foreach (var script in packageToUninstall.Variant.Scripts.PostUninstall)
            {
                _logger.LogDebug("Running script: {script}", script);
                if (!dryRun)
                {
                    await _commandRunner.Run(script, _pathManager.WorkingDir);
                }
            }
        }

        _logger.LogInformation("Package {packageSpecifier} uninstalled.", packageToUninstall.Specifier);
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
            return new StandaloneFileSource(_fileSystem, assetFile.FullName);
        }
        else
        {
            return new ArchiveFileSource(_fileSystem, assetFile.FullName);
        }
    }

    private void RemoveParentDirectoriesUntilWorkingDir(string path)
    {
        path = _fileSystem.Path.Join(_pathManager.WorkingDir, path);

        string? parentDir = _fileSystem.Path.GetDirectoryName(path);

        while (parentDir != null
               && parentDir.StartsWith(_pathManager.WorkingDir)
               && parentDir != _pathManager.WorkingDir)
        {
            if (_fileSystem.Directory.Exists(parentDir)
                && !_fileSystem.Directory.EnumerateFileSystemEntries(parentDir).Any())
            {
                _fileSystem.Directory.Delete(parentDir);
            }

            parentDir = _fileSystem.Path.GetDirectoryName(parentDir);
        }
    }

    private async Task SaveCurrentPackageLock(PackageLock packageLock)
    {
        using Stream packageLockFileStream = _fileSystem.File.Create(_pathManager.CurrentPackageLockPath);

        await packageLock.ToStream(packageLockFileStream);
    }
}