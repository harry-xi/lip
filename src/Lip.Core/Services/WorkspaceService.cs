using Lip.Core.Entities;
using Lip.Core.Infrastructure;

using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;

namespace Lip.Core.Services;

public interface IWorkspaceService
{
    enum PackageScope
    {
        All,
        Explicit,
        Implicit,
    }

    Task AddInstalledPackage(
        PackageSpec packageSpec,
        PackageManifest manifest,
        IEnumerable<IFileInfo> files,
        bool isExplicit);
    Task<IEnumerable<IFileInfo>> GetInstalledPackageFiles(PackageSpec packageSpec);
    Task<PackageManifest> GetInstalledPackageManifest(PackageSpec packageSpec);
    Task<IEnumerable<PackageSpec>> GetInstalledPackages(PackageScope scope);
    Task RemoveInstalledPackage(PackageSpec packageSpec);
    Task UpdateInstalledPackageExplicitness(PackageSpec packageSpec, bool isExplicit);
}

public class WorkspaceService(
    IFileSystem fileSystem,
    IUserInteraction userInteraction) : IWorkspaceService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IUserInteraction _userInteraction = userInteraction;

    public async Task AddInstalledPackage(
        PackageSpec packageSpec,
        PackageManifest manifest,
        IEnumerable<IFileInfo> files,
        bool isExplicit)
    {
        if (packageSpec.Id.Path != manifest.Path || packageSpec.Version != manifest.Version)
        {
            throw new ArgumentException(
                $"Package spec '{packageSpec}' does not match manifest path '{manifest.Path}' and version '{manifest.Version}'.", nameof(packageSpec));
        }

        WorkspaceStatePackage newPackage = new()
        {
            Files = [.. files.Select(f => f.FullName)],
            IsExplicit = isExplicit,
            Manifest = manifest,
            Variant = packageSpec.Id.Variant,
        };

        WorkspaceState state = await LoadWorkspaceState();

        if (state.Packages.Any(p => p.GetPackageSpec() == packageSpec))
        {
            throw new InvalidOperationException(
                $"Cannot add package '{packageSpec}' because it is already installed.");
        }

        WorkspaceState newState = state with
        {
            Packages = [.. state.Packages, newPackage]
        };

        await SaveWorkspaceState(newState);
    }

    public async Task<IEnumerable<IFileInfo>> GetInstalledPackageFiles(PackageSpec packageSpec)
    {
        WorkspaceState state = await LoadWorkspaceState();

        if (!state.Packages.Any(p => p.GetPackageSpec() == packageSpec))
        {
            throw new InvalidOperationException(
                $"Cannot get files of package '{packageSpec}' because it is not installed.");
        }

        return state.Packages
            .Single(p => p.GetPackageSpec() == packageSpec)
            .Files
            .Select(_fileSystem.FileInfo.New);
    }

    public async Task<PackageManifest> GetInstalledPackageManifest(PackageSpec packageSpec)
    {
        WorkspaceState state = await LoadWorkspaceState();

        if (!state.Packages.Any(p => p.GetPackageSpec() == packageSpec))
        {
            throw new InvalidOperationException(
                $"Cannot get manifest of package '{packageSpec}' because it is not installed.");
        }

        return state.Packages
            .Single(p => p.GetPackageSpec() == packageSpec)
            .Manifest;
    }

    public async Task<IEnumerable<PackageSpec>> GetInstalledPackages(
        IWorkspaceService.PackageScope scope)
    {
        WorkspaceState state = await LoadWorkspaceState();

        IEnumerable<PackageSpec> packages = scope switch
        {
            IWorkspaceService.PackageScope.All => state.Packages
                .Select(p => p.GetPackageSpec()),
            IWorkspaceService.PackageScope.Explicit => state.Packages
                .Where(p => p.IsExplicit)
                .Select(p => p.GetPackageSpec()),
            IWorkspaceService.PackageScope.Implicit => state.Packages
                .Where(p => !p.IsExplicit)
                .Select(p => p.GetPackageSpec()),
            _ => throw new UnreachableException(),
        };

        // Topologically sort packages.
        IEnumerable<DependencyNode> dependencyNodes = packages
            .Select(p => new DependencyNode(
                p,
                state.Packages
                    .Single(sp => sp.GetPackageSpec() == p)
                    .Manifest
                    .GetVariant(p.Id.Variant)
                    .Dependencies
                    .Select(d => new PackageReqt(d.Key, d.Value))));

        IEnumerable<PackageSpec> sortedPackages = IDependencySolver.TopologicalSort(dependencyNodes);

        return sortedPackages;
    }

    public async Task RemoveInstalledPackage(PackageSpec packageSpec)
    {
        WorkspaceState state = await LoadWorkspaceState();

        if (!state.Packages.Any(p => p.GetPackageSpec() == packageSpec))
        {
            throw new InvalidOperationException(
                $"Cannot remove package '{packageSpec}' because it is not installed.");
        }

        WorkspaceState newState = state with
        {
            Packages = [.. state.Packages.Where(p => p.GetPackageSpec() != packageSpec)]
        };

        await SaveWorkspaceState(newState);
    }

    public async Task UpdateInstalledPackageExplicitness(PackageSpec packageSpec, bool isExplicit)
    {
        WorkspaceState state = await LoadWorkspaceState();

        if (!state.Packages.Any(p => p.GetPackageSpec() == packageSpec))
        {
            throw new InvalidOperationException(
                $"Cannot update explicitness of package '{packageSpec}' because it is not installed.");
        }

        WorkspaceState newState = state with
        {
            Packages = [.. state.Packages
                .Select(p => p.GetPackageSpec() == packageSpec
                    ? p with { IsExplicit = isExplicit }
                    : p)]
        };

        await SaveWorkspaceState(newState);
    }

    private async Task<WorkspaceState> LoadWorkspaceState()
    {
        if (!_fileSystem.File.Exists("tooth_lock.json"))
        {
            await _userInteraction.PrintWarning("Workspace state file not found at 'tooth_lock.json'. Using default workspace state.");

            WorkspaceState state = new();

            await SaveWorkspaceState(state);

            return state;
        }

        try
        {
            using Stream readStream = _fileSystem.File.OpenRead("tooth_lock.json");

            WorkspaceState state = (await JsonSerializer.DeserializeAsync<WorkspaceState>(readStream))!;

            return state;
        }
        catch (Exception ex)
        {
            await _userInteraction.PrintError($"Failed to load workspace state from 'tooth_lock.json': {ex.Message}");
            await _userInteraction.PrintWarning("Using default workspace state.");

            WorkspaceState state = new();

            await SaveWorkspaceState(state);

            return state;
        }
    }

    private async Task SaveWorkspaceState(WorkspaceState state)
    {
        using Stream writeStream = _fileSystem.CreateFileWithDirectory("tooth_lock.json");

        await JsonSerializer.SerializeAsync(
            writeStream,
            state,
            _jsonSerializerOptions);
    }
}