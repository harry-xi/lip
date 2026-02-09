using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Lip.Core.SourceProviders;
using Microsoft.Extensions.Logging;
using Semver;
using System.Text.Json;

namespace Lip.Core.Services;

public interface IInstallService
{
    Task InstallPackages(
        IEnumerable<PackageSpec> packages,
        IEnumerable<PackageId> flexiblePackages,
        IEnumerable<LocalPackageSpec> localPackages,
        IEnumerable<RemotePackageSpec> remotePackages,
        bool dryRun,
        bool ignoreScripts,
        bool noDependencies);

    Task UninstallPackages(
        IEnumerable<PackageId> packages,
        bool dryRun,
        bool ignoreScripts,
        bool noDependencies);

    Task UpdatePackages(
        IEnumerable<PackageSpec> packages,
        IEnumerable<PackageId> flexiblePackages,
        IEnumerable<LocalPackageSpec> localPackages,
        IEnumerable<RemotePackageSpec> remotePackages,
        bool dryRun,
        bool ignoreScripts);
}

public class InstallService(
    ILogger logger,
    IPackageInstaller packageInstaller,
    IPackageRegistry packageRegistry,
    ISourceService sourceService,
    IWorkspaceService workspaceService) : IInstallService
{
    private readonly ILogger _logger = logger;

    private readonly IPackageInstaller _packageInstaller = packageInstaller;
    private readonly IPackageRegistry _packageRegistry = packageRegistry;
    private readonly ISourceService _sourceService = sourceService;
    private readonly IWorkspaceService _workspaceService = workspaceService;

    public async Task InstallPackages(
        IEnumerable<PackageSpec> packages,
        IEnumerable<PackageId> flexiblePackages,
        IEnumerable<LocalPackageSpec> localPackages,
        IEnumerable<RemotePackageSpec> remotePackages,
        bool dryRun,
        bool ignoreScripts,
        bool noDependencies)
    {
        IEnumerable<PackageArtifact> packageArtifacts = await ResolvePackageArtifacts(
            packages,
            flexiblePackages,
            localPackages,
            remotePackages);

        if (noDependencies)
        {
            _logger.LogWarning(
                "--no-dependencies is enabled. This operation may result in a broken workspace.");

            foreach (PackageArtifact packageArtifact in packageArtifacts)
            {
                await _packageInstaller.InstallPackage(packageArtifact, dryRun, explicitInstall: true, ignoreScripts);
            }

            return;
        }

        IEnumerable<PackageSpec> explicitlyInstalledPackages = await _workspaceService.GetInstalledPackages(
             IWorkspaceService.PackageScope.Explicit);

        if (packageArtifacts.FirstOrDefault(pa => explicitlyInstalledPackages.Any(ep => ep.Id == pa.Spec.Id)) is PackageArtifact pa)
        {
            throw new InvalidOperationException(
                $"Cannot install package '{pa.Spec.Id}' because it is already explicitly installed.");
        }

        CompositePackageRegistry packageRegistry = new(
        [
            new ArtifactsPackageRegistry(packageArtifacts),
            _packageRegistry
        ]);
        DependencySolver dependencySolver = new(_logger, packageRegistry);

        IEnumerable<PackageSpec> newExplicitlyInstalledPackages =
        [
            .. explicitlyInstalledPackages,
            .. packageArtifacts.Select(pa => pa.Spec)
        ]; // Not sorted.
        IEnumerable<PackageSpec> newInstalledPackages = await dependencySolver.Solve(newExplicitlyInstalledPackages
            .Select(p => new PackageReqt(p.Id, SemVersionRange.Equals(p.Version))));

        IEnumerable<PackageSpec> installedPackages = await _workspaceService.GetInstalledPackages(
            IWorkspaceService.PackageScope.All); // Topologically sorted.

        // Uninstall packages that are no longer needed.
        foreach (PackageSpec installedPackage in installedPackages.Reverse())
        {
            if (!newInstalledPackages.Contains(installedPackage))
            {
                await _packageInstaller.UninstallPackage(installedPackage.Id, dryRun, ignoreScripts);
            }
        }

        // Install new packages in topological order.
        foreach (PackageSpec newPackage in newInstalledPackages)
        {
            if (!installedPackages.Contains(newPackage))
            {
                PackageArtifact packageArtifact = packageArtifacts.FirstOrDefault(pa => pa.Spec == newPackage)
                    ?? new(newPackage, await _sourceService.Get(newPackage));

                await _packageInstaller.InstallPackage(
                    packageArtifact,
                    dryRun,
                    explicitInstall: newExplicitlyInstalledPackages.Contains(newPackage),
                    ignoreScripts);
            }
        }
    }

    public async Task UninstallPackages(
        IEnumerable<PackageId> packages,
        bool dryRun,
        bool ignoreScripts,
        bool noDependencies)
    {
        if (noDependencies)
        {
            _logger.LogWarning(
                "--no-dependencies is enabled. This operation may result in a broken workspace.");

            foreach (PackageId package in packages)
            {
                await _packageInstaller.UninstallPackage(package, dryRun, ignoreScripts);
            }

            return;
        }

        DependencySolver dependencySolver = new(_logger, _packageRegistry);

        IEnumerable<PackageSpec> explicitlyInstalledPackages = await _workspaceService.GetInstalledPackages(
            IWorkspaceService.PackageScope.Explicit);

        if (packages.FirstOrDefault(p => !explicitlyInstalledPackages.Any(ep => ep.Id == p)) is PackageId packageId)
        {
            throw new InvalidOperationException(
                $"Package '{packageId}' is not explicitly installed and cannot be uninstalled without --no-dependencies.");
        }

        IEnumerable<PackageSpec> newExplicitlyInstalledPackages = explicitlyInstalledPackages
            .Where(p => !packages.Contains(p.Id));
        IEnumerable<PackageSpec> newInstalledPackages = await dependencySolver.Solve(newExplicitlyInstalledPackages
            .Select(p => new PackageReqt(p.Id, SemVersionRange.Equals(p.Version))));

        IEnumerable<PackageSpec> installedPackages = await _workspaceService.GetInstalledPackages(
            IWorkspaceService.PackageScope.All); // Topologically sorted.

        foreach (PackageSpec installedPackage in installedPackages.Reverse())
        {
            if (!newInstalledPackages.Contains(installedPackage))
            {
                await _packageInstaller.UninstallPackage(installedPackage.Id, dryRun, ignoreScripts);
            }
        }
    }

    public async Task UpdatePackages(
        IEnumerable<PackageSpec> packages,
        IEnumerable<PackageId> flexiblePackages,
        IEnumerable<LocalPackageSpec> localPackages,
        IEnumerable<RemotePackageSpec> remotePackages,
        bool dryRun,
        bool ignoreScripts)
    {
        IEnumerable<PackageArtifact> packageArtifacts = await ResolvePackageArtifacts(
            packages,
            flexiblePackages,
            localPackages,
            remotePackages);

        IEnumerable<PackageSpec> installedPackages = await _workspaceService.GetInstalledPackages(
            IWorkspaceService.PackageScope.All); // Topologically sorted.

        if (packageArtifacts.FirstOrDefault(pa => !installedPackages.Any(ip => ip.Id == pa.Spec.Id)) is PackageArtifact pa)
        {
            throw new InvalidOperationException(
                $"Cannot update package '{pa.Spec.Id}' because it is not currently installed.");
        }

        CompositePackageRegistry packageRegistry = new(
        [
            new ArtifactsPackageRegistry(packageArtifacts),
            _packageRegistry
        ]);
        DependencySolver dependencySolver = new(_logger, packageRegistry);

        IEnumerable<PackageSpec> explicitlyInstalledPackages = await _workspaceService.GetInstalledPackages(
            IWorkspaceService.PackageScope.Explicit);

        IEnumerable<PackageSpec> newExplicitlyInstalledPackages = [
            .. explicitlyInstalledPackages.Where(ep => !packageArtifacts.Any(pa => pa.Spec.Id == ep.Id)),
            .. packageArtifacts.Select(pa => pa.Spec)
        ]; // Not sorted.

        IEnumerable<PackageSpec> newInstalledPackages = await dependencySolver.Solve(newExplicitlyInstalledPackages
            .Select(p => new PackageReqt(p.Id, SemVersionRange.Equals(p.Version))));

        // Uninstall packages that are no longer needed.
        foreach (PackageSpec installedPackage in installedPackages.Reverse())
        {
            if (!newInstalledPackages.Contains(installedPackage))
            {
                await _packageInstaller.UninstallPackage(installedPackage.Id, dryRun, ignoreScripts);
            }
        }

        // Install new packages in topological order.
        foreach (PackageSpec newPackage in newInstalledPackages)
        {
            if (!installedPackages.Contains(newPackage))
            {
                PackageArtifact packageArtifact = packageArtifacts.FirstOrDefault(pa => pa.Spec == newPackage)
                    ?? new(newPackage, await _sourceService.Get(newPackage));

                await _packageInstaller.InstallPackage(
                    packageArtifact,
                    dryRun,
                    explicitInstall: newExplicitlyInstalledPackages.Contains(newPackage),
                    ignoreScripts);
            }
        }
    }

    private async Task<IEnumerable<PackageArtifact>> ResolvePackageArtifacts(
        IEnumerable<PackageSpec> packages,
        IEnumerable<PackageId> flexiblePackages,
        IEnumerable<LocalPackageSpec> localPackages,
        IEnumerable<RemotePackageSpec> remotePackages)
    {
        List<PackageArtifact> packageArtifacts = [];

        foreach (PackageSpec packageSpec in packages)
        {
            ISourceProvider sourceProvider = await _sourceService.Get(packageSpec);
            PackageArtifact packageArtifact = new(packageSpec, sourceProvider);

            packageArtifacts.Add(packageArtifact);
        }

        foreach (PackageId packageId in flexiblePackages)
        {
            IEnumerable<SemVersion> versions = await _packageRegistry.GetAvailableVersions(packageId);
            SemVersion latestVersion = versions.Max()
                ?? throw new InvalidOperationException($"Failed to find the latest version for package '{packageId}'.");
            PackageSpec packageSpec = new(packageId, latestVersion);
            ISourceProvider sourceProvider = await _sourceService.Get(packageSpec);
            PackageArtifact packageArtifact = new(packageSpec, sourceProvider);

            packageArtifacts.Add(packageArtifact);
        }

        foreach (LocalPackageSpec localPackageSpec in localPackages)
        {
            ISourceProvider sourceProvider = await _sourceService.Get(localPackageSpec);

            using Stream manifestStream = await sourceProvider.OpenRead("tooth.json");
            PackageManifest manifest = (await JsonSerializer.DeserializeAsync<PackageManifest>(manifestStream))!;

            PackageId packageId = new(manifest.Path, localPackageSpec.Variant);
            PackageSpec packageSpec = new(packageId, manifest.Version);
            PackageArtifact packageArtifact = new(packageSpec, sourceProvider);

            packageArtifacts.Add(packageArtifact);
        }

        foreach (RemotePackageSpec remotePackageSpec in remotePackages)
        {
            ISourceProvider sourceProvider = await _sourceService.Get(remotePackageSpec);

            using Stream manifestStream = await sourceProvider.OpenRead("tooth.json");
            PackageManifest manifest = (await JsonSerializer.DeserializeAsync<PackageManifest>(manifestStream))!;

            PackageId packageId = new(manifest.Path, remotePackageSpec.Variant);
            PackageSpec packageSpec = new(packageId, manifest.Version);
            PackageArtifact packageArtifact = new(packageSpec, sourceProvider);

            packageArtifacts.Add(packageArtifact);
        }

        // Check for duplicate package IDs.
        if (packageArtifacts
            .GroupBy(pa => pa.Spec.Id)
            .FirstOrDefault(g => g.Count() > 1) is IGrouping<PackageId, PackageArtifact> duplicateGroup)
        {
            throw new InvalidOperationException(
                $"Multiple packages with ID '{duplicateGroup.Key}' were specified. Please ensure that each package ID is unique.");
        }

        return packageArtifacts;
    }
}