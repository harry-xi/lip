using DotNet.Globbing;
using Flurl;
using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.SourceProviders;

using System.Diagnostics;
using System.IO.Abstractions;

namespace Lip.Core.Services;

public interface IPackageInstaller
{
    Task InstallPackage(
        PackageArtifact packageArtifact,
        bool dryRun,
        bool explicitInstall,
        bool ignoreScripts);

    Task UninstallPackage(
        PackageId packageId,
        bool dryRun,
        bool ignoreScripts);
}

public class PackageInstaller(
    ICommandRunner commandRunner,
    IFileSystem fileSystem,
    IUserInteraction userInteraction,
    ISourceService sourceService,
    IWorkspaceService workspaceService) : IPackageInstaller
{
    private readonly ICommandRunner _commandRunner = commandRunner;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IUserInteraction _userInteraction = userInteraction;

    private readonly ISourceService _sourceService = sourceService;
    private readonly IWorkspaceService _workspaceService = workspaceService;

    public async Task InstallPackage(
        PackageArtifact packageArtifact,
        bool dryRun,
        bool explicitInstall,
        bool ignoreScripts)
    {
        IEnumerable<PackageSpec> installedPackages = await _workspaceService.GetInstalledPackages(
            IWorkspaceService.PackageScope.All);

        if (installedPackages.FirstOrDefault(p => p.Id == packageArtifact.Spec.Id) is PackageSpec existingPackageSpec)
        {
            throw new InvalidOperationException($"Cannot install package {packageArtifact.Spec.Id} version {packageArtifact.Spec.Version} because it is already installed with version {existingPackageSpec.Version}");
        }

        if (dryRun)
        {
            await _userInteraction.PrintInfo(
                $"Dry run: would install package {packageArtifact.Spec.Id} version {packageArtifact.Spec.Version}");

            return;
        }

        await _userInteraction.PrintInfo(
            $"Installing package {packageArtifact.Spec.Id} version {packageArtifact.Spec.Version}");

        using Stream manifestStream = await packageArtifact.SourceProvider.OpenRead("tooth.json");
        PackageManifest manifest = await PackageManifest.FromStream(manifestStream);
        PackageManifestVariant variant = manifest.GetVariant(packageArtifact.Spec.Id.Variant);

        // Step 1: Run pre-install scripts.

        if (!ignoreScripts)
        {
            foreach (string script in variant.Scripts.PreInstall)
            {
                await _commandRunner.Run(script);
            }
        }

        // Step 2: Place files.

        List<IFileInfo> placedFiles = [];

        foreach (PackageManifestAsset asset in variant.Assets)
        {
            ISourceProvider assetSourceProvider = asset.Type switch
            {
                PackageManifestAsset.AssetType.Self => packageArtifact.SourceProvider,
                PackageManifestAsset.AssetType.Uncompressed => await GetSourceProvider(asset.Urls, isArchive: false),
                PackageManifestAsset.AssetType.Tar => await GetSourceProvider(asset.Urls, isArchive: true),
                PackageManifestAsset.AssetType.Tgz => await GetSourceProvider(asset.Urls, isArchive: true),
                PackageManifestAsset.AssetType.Zip => await GetSourceProvider(asset.Urls, isArchive: true),
                _ => throw new UnreachableException(),
            };

            foreach (string key in assetSourceProvider.Keys)
            {
                List<IFileInfo> targetLocations = [];
                foreach (PackageManifestAssetPlacement placement in asset.Placements)
                {
                    switch (placement.Type)
                    {
                        case PackageManifestAssetPlacement.PlacementType.File:
                            if (placement.Src == key)
                            {
                                targetLocations.Add(_fileSystem.FileInfo.New(placement.Dst));
                            }
                            else if (Glob.Parse(placement.Src).IsMatch(key))
                            {
                                string targetPath = _fileSystem.Path.Combine(
                                    placement.Dst,
                                    Path.GetFileName(key));
                                targetLocations.Add(_fileSystem.FileInfo.New(targetPath));
                            }

                            break;

                        case PackageManifestAssetPlacement.PlacementType.Directory:
                            if (Path.GetRelativePath(placement.Src, key) is string relativePath
                                && !relativePath.StartsWith(".."))
                            {
                                string targetPath = _fileSystem.Path.Combine(
                                    placement.Dst,
                                    relativePath);
                                targetLocations.Add(_fileSystem.FileInfo.New(targetPath));
                            }

                            break;

                        default:
                            throw new UnreachableException();
                    }
                }

                foreach (IFileInfo targetLocation in targetLocations)
                {
                    using Stream sourceStream = await assetSourceProvider.OpenRead(key);
                    using Stream targetStream = _fileSystem.CreateFileWithDirectory(targetLocation.FullName);
                    await sourceStream.CopyToAsync(targetStream);

                    placedFiles.Add(targetLocation);
                }
            }
        }

        // Step 3: Run post-install scripts.

        if (!ignoreScripts)
        {
            foreach (string script in variant.Scripts.PostInstall)
            {
                await _commandRunner.Run(script);
            }
        }

        // Step 4: Add package to workspace state.

        await _workspaceService.AddInstalledPackage(
            packageArtifact.Spec,
            manifest,
            placedFiles,
            explicitInstall);
    }

    public async Task UninstallPackage(
        PackageId packageId,
        bool dryRun,
        bool ignoreScripts)
    {
        IEnumerable<PackageSpec> installedPackages = await _workspaceService.GetInstalledPackages(
            IWorkspaceService.PackageScope.All);


        PackageSpec existingPackageSpec = installedPackages.FirstOrDefault(p => p.Id == packageId)
            ?? throw new InvalidOperationException($"Cannot uninstall package {packageId} because it is not installed");

        if (dryRun)
        {
            await _userInteraction.PrintInfo(
                $"Dry run: would uninstall package {existingPackageSpec.Id} version {existingPackageSpec.Version}");

            return;
        }

        await _userInteraction.PrintInfo(
            $"Uninstalling package {existingPackageSpec.Id} version {existingPackageSpec.Version}");

        PackageManifest manifest = await _workspaceService.GetInstalledPackageManifest(existingPackageSpec);
        PackageManifestVariant variant = manifest.GetVariant(existingPackageSpec.Id.Variant);

        // Step 1: Run pre-uninstall scripts.

        if (!ignoreScripts)
        {
            foreach (string script in variant.Scripts.PreUninstall)
            {
                await _commandRunner.Run(script);
            }
        }

        // Step 2: Remove placed files.

        foreach (IFileInfo file in await _workspaceService.GetInstalledPackageFiles(existingPackageSpec))
        {
            if (variant.PreserveFiles.Any(preserveGlob => preserveGlob.IsMatch(file.Name)))
            {
                continue;
            }

            file.Delete();
        }

        // Step 3: Remove the files specified to be removed.

        foreach (Glob glob in variant.RemoveFiles)
        {
            foreach (string path in _fileSystem.Directory.EnumerateFileSystemEntries(
                ".",
                glob.ToString(),
                SearchOption.AllDirectories))
            {
                _fileSystem.File.Delete(path);
            }
        }

        // Step 4: Run post-uninstall scripts.

        if (!ignoreScripts)
        {
            foreach (string script in variant.Scripts.PostUninstall)
            {
                await _commandRunner.Run(script);
            }
        }

        // Step 4: Remove package from workspace state.

        await _workspaceService.RemoveInstalledPackage(existingPackageSpec);
    }

    private async Task<ISourceProvider> GetSourceProvider(IEnumerable<Url> urls, bool isArchive)
    {
        List<Exception> exceptions = [];

        foreach (Url url in urls)
        {
            try
            {
                return await _sourceService.Get(url, isArchive);
            }
            catch (Exception ex)
            {
                exceptions.Add(new Exception($"Failed to get file source from {url}", ex));
            }
        }

        throw new AggregateException("Failed to get file source from all provided URLs", exceptions);
    }
}