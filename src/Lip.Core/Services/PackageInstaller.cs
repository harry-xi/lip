using DotNet.Globbing;
using Flurl;
using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.SourceProviders;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;

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
    ILogger logger,
    ISourceService sourceService,
    IWorkspaceService workspaceService) : IPackageInstaller
{
    private readonly ICommandRunner _commandRunner = commandRunner;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ILogger _logger = logger;

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

        PackageSpec existingPackageSpec = installedPackages.FirstOrDefault(p => p.Id == packageArtifact.Spec.Id)
            ?? throw new InvalidOperationException(
                $"Cannot install package {packageArtifact.Spec.Id} version {packageArtifact.Spec.Version} because it is not already installed. Use explicitInstall to force installation.");

        if (dryRun)
        {
            _logger.LogInformation(
                "Dry run: would install package {PackageId} version {PackageVersion}",
                packageArtifact.Spec.Id,
                packageArtifact.Spec.Version);

            return;
        }

        _logger.LogInformation(
            "Installing package {PackageId} version {PackageVersion}",
            packageArtifact.Spec.Id,
            packageArtifact.Spec.Version);

        using Stream manifestStream = await packageArtifact.SourceProvider.OpenRead("tooth.json");
        PackageManifest manifest = (await JsonSerializer.DeserializeAsync<PackageManifest>(manifestStream))!;
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
                PackageManifestAsset.AssetType.Uncompressed => await GetSourceProvider(asset.Urls, ISourceService.ParsingMode.Single),
                PackageManifestAsset.AssetType.Tar => await GetSourceProvider(asset.Urls, ISourceService.ParsingMode.Composite),
                PackageManifestAsset.AssetType.Tgz => await GetSourceProvider(asset.Urls, ISourceService.ParsingMode.Composite),
                PackageManifestAsset.AssetType.Zip => await GetSourceProvider(asset.Urls, ISourceService.ParsingMode.Composite),
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
                            throw new ArgumentException($"Unsupported placement type: {placement.Type}");
                    }
                }

                foreach (IFileInfo targetLocation in targetLocations)
                {
                    _fileSystem.CreateFileWithDirectory(targetLocation.FullName);

                    using Stream sourceStream = await assetSourceProvider.OpenRead(key);
                    using Stream targetStream = targetLocation.Create();
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
            _logger.LogInformation(
                "Dry run: would uninstall package {PackageId} version {PackageVersion}",
                existingPackageSpec.Id,
                existingPackageSpec.Version);

            return;
        }

        _logger.LogInformation(
            "Uninstalling package {PackageId} version {PackageVersion}",
            existingPackageSpec.Id,
            existingPackageSpec.Version);

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
                "",
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

    private async Task<ISourceProvider> GetSourceProvider(
        IEnumerable<Url> urls,
        ISourceService.ParsingMode parsingMode)
    {
        List<Exception> exceptions = [];

        foreach (Url url in urls)
        {
            try
            {
                return await _sourceService.Get(url, parsingMode);
            }
            catch (Exception ex)
            {
                exceptions.Add(new Exception($"Failed to get file source from {url}", ex));
            }
        }

        throw new AggregateException("Failed to get file source from all provided URLs", exceptions);
    }
}