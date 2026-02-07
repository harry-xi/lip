using Lip.Core.Entities;
using Lip.Core.Extensions;
using Microsoft.Extensions.Logging;
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

    Task<IEnumerable<PackageSpec>> GetInstalledPackages(PackageScope scope);
}

public class WorkspaceService(IFileSystem fileSystem, ILogger logger) : IWorkspaceService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ILogger _logger = logger;

    public async Task<IEnumerable<PackageSpec>> GetInstalledPackages(
        IWorkspaceService.PackageScope scope)
    {
        WorkspaceState state = await LoadWorkspaceState();

        return scope switch
        {
            IWorkspaceService.PackageScope.All => state.Packages
                .Select(p => p.GetPackageSpec()),
            IWorkspaceService.PackageScope.Explicit => state.Packages
                .Where(p => p.IsExplicit)
                .Select(p => p.GetPackageSpec()),
            IWorkspaceService.PackageScope.Implicit => state.Packages
                .Where(p => !p.IsExplicit)
                .Select(p => p.GetPackageSpec()),
            _ => throw new NotImplementedException(),
        };
    }

    private async Task<WorkspaceState> LoadWorkspaceState()
    {
        if (!_fileSystem.File.Exists("tooth_lock.json"))
        {
            _logger.LogInformation("Workspace state file not found at 'tooth_lock.json'. Using default workspace state.");

            WorkspaceState state = new();

            await SaveWorkspaceState(state);

            return state;
        }

        try
        {
            using Stream readStream = _fileSystem.File.OpenRead("tooth_lock.json");

            WorkspaceState state = await JsonSerializer.DeserializeAsync<WorkspaceState>(readStream)
                ?? throw new JsonException("Failed to deserialize WorkspaceState");

            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workspace state from 'tooth_lock.json'.");
            _logger.LogInformation("Using default workspace state.");

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