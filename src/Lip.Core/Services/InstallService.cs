using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.PackageRegistries;
using Lip.Core.Sources;

using Semver;

namespace Lip.Core.Services;

public interface IInstallService {
  Task<PackageSpec> GetLatestVersion(PackageId packageId);

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
    IUserInteraction userInteraction,
    IPackageInstaller packageInstaller,
    IPackageRegistry packageRegistry,
    ISourceService sourceService,
    IWorkspaceService workspaceService) : IInstallService {
  private readonly IUserInteraction _userInteraction = userInteraction;

  private readonly IPackageInstaller _packageInstaller = packageInstaller;
  private readonly IPackageRegistry _packageRegistry = packageRegistry;
  private readonly ISourceService _sourceService = sourceService;
  private readonly IWorkspaceService _workspaceService = workspaceService;

  public async Task<PackageSpec> GetLatestVersion(PackageId packageId) {
    IEnumerable<SemVersion> versions = await _packageRegistry.GetAvailableVersions(packageId);

    SemVersion latestVersion = versions.Max(SemVersion.PrecedenceComparer)
        ?? throw new InvalidOperationException($"Failed to find the latest version for package '{packageId}'.");

    PackageSpec packageSpec = new(packageId, latestVersion);

    return packageSpec;
  }

  public async Task InstallPackages(
      IEnumerable<PackageSpec> packages,
      IEnumerable<PackageId> flexiblePackages,
      IEnumerable<LocalPackageSpec> localPackages,
      IEnumerable<RemotePackageSpec> remotePackages,
      bool dryRun,
      bool ignoreScripts,
      bool noDependencies) {
    IEnumerable<PackageArtifact> packageArtifacts = await ResolvePackageArtifacts(
        packages,
        flexiblePackages,
        localPackages,
        remotePackages);

    if (noDependencies) {
      await _userInteraction.PrintWarning(
          "--no-dependencies is enabled. This operation may result in a broken workspace.");

      foreach (PackageArtifact packageArtifact in packageArtifacts) {
        await _packageInstaller.InstallPackage(packageArtifact, dryRun, explicitInstall: true, ignoreScripts);
      }

      return;
    }

    IEnumerable<PackageSpec> explicitlyInstalledPackages = await _workspaceService.GetInstalledPackages(
         IWorkspaceService.PackageScope.Explicit);

    if (packageArtifacts.FirstOrDefault(pa => explicitlyInstalledPackages.Any(ep => ep.Id == pa.Spec.Id)) is PackageArtifact pa) {
      throw new InvalidOperationException(
          $"Cannot install package '{pa.Spec.Id}' because it is already explicitly installed.");
    }

    CompositePackageRegistry packageRegistry = new(
    [
        [new ArtifactsPackageRegistry(packageArtifacts)],
        [_packageRegistry]
    ]);
    DependencySolver dependencySolver = new(packageRegistry, _userInteraction);

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
    foreach (PackageSpec installedPackage in installedPackages.Reverse()) {
      if (!newInstalledPackages.Contains(installedPackage)) {
        await _packageInstaller.UninstallPackage(installedPackage.Id, dryRun, ignoreScripts);
      }
    }

    // Install new packages in topological order.
    foreach (PackageSpec newPackage in newInstalledPackages) {
      if (!installedPackages.Contains(newPackage)) {
        PackageArtifact packageArtifact = packageArtifacts.FirstOrDefault(pa => pa.Spec == newPackage)
            ?? new(newPackage, await _sourceService.Get(newPackage));

        await _packageInstaller.InstallPackage(
            packageArtifact,
            dryRun,
            explicitInstall: newExplicitlyInstalledPackages.Contains(newPackage),
            ignoreScripts);
      }
    }

    await ReconcilePackageExplicitness(
        newInstalledPackages,
        newExplicitlyInstalledPackages,
        explicitlyInstalledPackages,
        dryRun);
  }

  public async Task UninstallPackages(
      IEnumerable<PackageId> packages,
      bool dryRun,
      bool ignoreScripts,
      bool noDependencies) {
    if (noDependencies) {
      await _userInteraction.PrintWarning(
          "--no-dependencies is enabled. This operation may result in a broken workspace.");

      foreach (PackageId package in packages) {
        await _packageInstaller.UninstallPackage(package, dryRun, ignoreScripts);
      }

      return;
    }

    DependencySolver dependencySolver = new(_packageRegistry, _userInteraction);

    IEnumerable<PackageSpec> explicitlyInstalledPackages = await _workspaceService.GetInstalledPackages(
        IWorkspaceService.PackageScope.Explicit);

    if (packages.FirstOrDefault(p => !explicitlyInstalledPackages.Any(ep => ep.Id == p)) is PackageId packageId) {
      throw new InvalidOperationException(
          $"Package '{packageId}' is not explicitly installed and cannot be uninstalled without --no-dependencies.");
    }

    IEnumerable<PackageSpec> newExplicitlyInstalledPackages = explicitlyInstalledPackages
        .Where(p => !packages.Contains(p.Id));
    IEnumerable<PackageSpec> newInstalledPackages = await dependencySolver.Solve(newExplicitlyInstalledPackages
        .Select(p => new PackageReqt(p.Id, SemVersionRange.Equals(p.Version))));

    IEnumerable<PackageSpec> installedPackages = await _workspaceService.GetInstalledPackages(
        IWorkspaceService.PackageScope.All); // Topologically sorted.

    foreach (PackageSpec installedPackage in installedPackages.Reverse()) {
      if (!newInstalledPackages.Contains(installedPackage)) {
        await _packageInstaller.UninstallPackage(installedPackage.Id, dryRun, ignoreScripts);
      }
    }

    await ReconcilePackageExplicitness(
        newInstalledPackages,
        newExplicitlyInstalledPackages,
        explicitlyInstalledPackages,
        dryRun);
  }

  public async Task UpdatePackages(
      IEnumerable<PackageSpec> packages,
      IEnumerable<PackageId> flexiblePackages,
      IEnumerable<LocalPackageSpec> localPackages,
      IEnumerable<RemotePackageSpec> remotePackages,
      bool dryRun,
      bool ignoreScripts) {
    IEnumerable<PackageArtifact> packageArtifacts = await ResolvePackageArtifacts(
        packages,
        flexiblePackages,
        localPackages,
        remotePackages);

    IEnumerable<PackageSpec> installedPackages = await _workspaceService.GetInstalledPackages(
        IWorkspaceService.PackageScope.All); // Topologically sorted.

    if (packageArtifacts.FirstOrDefault(pa => !installedPackages.Any(ip => ip.Id == pa.Spec.Id)) is PackageArtifact pa) {
      throw new InvalidOperationException(
          $"Cannot update package '{pa.Spec.Id}' because it is not currently installed.");
    }

    CompositePackageRegistry packageRegistry = new(
    [
      [new ArtifactsPackageRegistry(packageArtifacts)],
      [_packageRegistry]
    ]);
    DependencySolver dependencySolver = new(packageRegistry, _userInteraction);

    IEnumerable<PackageSpec> explicitlyInstalledPackages = await _workspaceService.GetInstalledPackages(
        IWorkspaceService.PackageScope.Explicit);

    IEnumerable<PackageSpec> newExplicitlyInstalledPackages = [
        .. explicitlyInstalledPackages.Where(ep => !packageArtifacts.Any(pa => pa.Spec.Id == ep.Id)),
            .. packageArtifacts.Select(pa => pa.Spec)
    ]; // Not sorted.

    IEnumerable<PackageSpec> newInstalledPackages = await dependencySolver.Solve(newExplicitlyInstalledPackages
        .Select(p => new PackageReqt(p.Id, SemVersionRange.Equals(p.Version))));

    // Uninstall packages that are no longer needed.
    foreach (PackageSpec installedPackage in installedPackages.Reverse()) {
      if (!newInstalledPackages.Contains(installedPackage)) {
        await _packageInstaller.UninstallPackage(installedPackage.Id, dryRun, ignoreScripts);
      }
    }

    // Install new packages in topological order.
    foreach (PackageSpec newPackage in newInstalledPackages) {
      if (!installedPackages.Contains(newPackage)) {
        PackageArtifact packageArtifact = packageArtifacts.FirstOrDefault(pa => pa.Spec == newPackage)
            ?? new(newPackage, await _sourceService.Get(newPackage));

        await _packageInstaller.InstallPackage(
            packageArtifact,
            dryRun,
            explicitInstall: newExplicitlyInstalledPackages.Contains(newPackage),
            ignoreScripts);
      }
    }

    await ReconcilePackageExplicitness(
        newInstalledPackages,
        newExplicitlyInstalledPackages,
        explicitlyInstalledPackages,
        dryRun);
  }

  private async Task ReconcilePackageExplicitness(
      IEnumerable<PackageSpec> installedPackages,
      IEnumerable<PackageSpec> explicitlyInstalledPackages,
      IEnumerable<PackageSpec> previousExplicitlyInstalledPackages,
      bool dryRun) {
    foreach (PackageSpec newPackage in installedPackages) {
      bool isExplicit = explicitlyInstalledPackages.Contains(newPackage);
      bool wasExplicit = previousExplicitlyInstalledPackages.Contains(newPackage);

      if (isExplicit && !wasExplicit) {
        if (dryRun) {
          await _userInteraction.PrintInfo(
              $"Dry run: would mark package '{newPackage.Id}' version {newPackage.Version} as explicitly installed.");

          continue;
        }

        await _userInteraction.PrintInfo(
            $"Marking package '{newPackage.Id}' version {newPackage.Version} as explicitly installed.");

        await _workspaceService.UpdateInstalledPackageExplicitness(newPackage, isExplicit: true);

      } else if (!isExplicit && wasExplicit) {
        if (dryRun) {
          await _userInteraction.PrintInfo(
              $"Dry run: would mark package '{newPackage.Id}' version {newPackage.Version} as not explicitly installed.");

          continue;
        }

        await _userInteraction.PrintInfo(
            $"Marking package '{newPackage.Id}' version {newPackage.Version} as not explicitly installed.");

        await _workspaceService.UpdateInstalledPackageExplicitness(newPackage, isExplicit: false);
      }
    }
  }

  private async Task<IEnumerable<PackageArtifact>> ResolvePackageArtifacts(
      IEnumerable<PackageSpec> packages,
      IEnumerable<PackageId> flexiblePackages,
      IEnumerable<LocalPackageSpec> localPackages,
      IEnumerable<RemotePackageSpec> remotePackages) {
    List<PackageArtifact> packageArtifacts = [];

    foreach (PackageSpec packageSpec in packages) {
      ISource source = await _sourceService.Get(packageSpec);
      PackageArtifact packageArtifact = new(packageSpec, source);

      packageArtifacts.Add(packageArtifact);
    }

    foreach (PackageId packageId in flexiblePackages) {
      await _userInteraction.PrintInfo($"Getting available versions for package '{packageId}'...");

      PackageSpec latestPackageSpec = await GetLatestVersion(packageId);
      ISource source = await _sourceService.Get(latestPackageSpec);
      PackageArtifact packageArtifact = new(latestPackageSpec, source);

      packageArtifacts.Add(packageArtifact);
    }

    foreach (LocalPackageSpec localPackageSpec in localPackages) {
      ISource source = await _sourceService.Get(localPackageSpec);

      using Stream manifestStream = await source.OpenRead("tooth.json");
      PackageManifest manifest = await PackageManifest.FromStream(manifestStream);

      PackageId packageId = new(manifest.Path, localPackageSpec.Variant);
      PackageSpec packageSpec = new(packageId, manifest.Version);
      PackageArtifact packageArtifact = new(packageSpec, source);

      packageArtifacts.Add(packageArtifact);
    }

    foreach (RemotePackageSpec remotePackageSpec in remotePackages) {
      ISource source = await _sourceService.Get(remotePackageSpec);

      using Stream manifestStream = await source.OpenRead("tooth.json");
      PackageManifest manifest = await PackageManifest.FromStream(manifestStream);

      PackageId packageId = new(manifest.Path, remotePackageSpec.Variant);
      PackageSpec packageSpec = new(packageId, manifest.Version);
      PackageArtifact packageArtifact = new(packageSpec, source);

      packageArtifacts.Add(packageArtifact);
    }

    // Check for duplicate package IDs.
    if (packageArtifacts
        .GroupBy(pa => pa.Spec.Id)
        .FirstOrDefault(g => g.Count() > 1) is IGrouping<PackageId, PackageArtifact> duplicateGroup) {
      throw new InvalidOperationException(
          $"Multiple packages with ID '{duplicateGroup.Key}' were specified. Please ensure that each package ID is unique.");
    }

    return packageArtifacts;
  }
}