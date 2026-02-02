using Lip.Core.Context;
using Microsoft.Extensions.Logging;
using Semver;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Lip.Core.Services;

public class InitService
{
    private readonly IContext _context;
    private readonly IPackageManager _packageManager;
    private readonly IPathManager _pathManager;

    public InitService(IContext context)
    {
        _context = context;

        var runtimeConfig = RuntimeConfig.Load(context.FileSystem);

        _pathManager = new PathManager(
            context.FileSystem,
            runtimeConfig.Cache,
            context.WorkingDir);

        var cacheManager = new CacheManager(
            context,
            _pathManager,
            runtimeConfig.GitHubProxies.ConvertAll(Flurl.Url.Parse),
            runtimeConfig.GoModuleProxies.ConvertAll(Flurl.Url.Parse));

        _packageManager = new PackageManager(
            context.FileSystem,
            context.CommandRunner,
            context.Logger,
            context.UserInteraction,
            cacheManager,
            _pathManager);
    }

    internal InitService(IContext context, IPackageManager packageManager, IPathManager pathManager)
    {
        _context = context;
        _packageManager = packageManager;
        _pathManager = pathManager;
    }

    private const string DefaultTooth = "example.com/org/package";
    private const string DefaultVersion = "0.1.0";

    public async Task Init(
        bool force = false,
        string? initAvatarUrl = null,
        string? initDescription = null,
        string? initName = null,
        string? initTooth = null,
        string? initVersion = null,
        bool yes = false)
    {
        PackageManifest manifest;

        if (yes)
        {
            manifest = new()
            {
                ToothPath = initTooth ?? DefaultTooth,
                Version = SemVersion.Parse(initVersion ?? DefaultVersion),
                Info = new()
                {
                    Name = initName ?? "",
                    Description = initDescription ?? "",
                    Tags = [],
                    AvatarUrl = initAvatarUrl
                },
                Variants = [
                    new PackageManifest.Variant
                    {
                        Label = "",
                        Platform = RuntimeInformation.RuntimeIdentifier,
                        Dependencies = [],
                        Assets = [],
                        PreserveFiles = [],
                        RemoveFiles = [],
                        Scripts = new PackageManifest.ScriptsType
                        {
                            PreInstall = [],
                            Install = [],
                            PostInstall  = [],
                            PreUninstall  = [],
                            Uninstall  = [],
                            PostUninstall  = [],
                        }
                    }
                ]
            };
        }
        else
        {
            string tooth = initTooth ?? await _context.UserInteraction.PromptForInput(DefaultTooth, "Enter the tooth path");
            string version = initVersion ?? await _context.UserInteraction.PromptForInput(DefaultVersion, "Enter the package version");
            string name = initName ?? await _context.UserInteraction.PromptForInput(string.Empty, "Enter the package name");
            string description = initDescription ?? await _context.UserInteraction.PromptForInput(string.Empty, "Enter the package description");
            string avatarUrl = initAvatarUrl ?? await _context.UserInteraction.PromptForInput(string.Empty, "Enter the package's avatar URL");

            manifest = new()
            {
                ToothPath = tooth,
                Version = SemVersion.Parse(version),
                Info = new()
                {
                    Name = name,
                    Description = description,
                    Tags = [],
                    AvatarUrl = avatarUrl
                },
                Variants = [
                    new PackageManifest.Variant
                    {
                        Label = "",
                        Platform = RuntimeInformation.RuntimeIdentifier,
                        Dependencies = [],
                        Assets = [],
                        PreserveFiles = [],
                        RemoveFiles = [],
                        Scripts = new PackageManifest.ScriptsType
                        {
                            PreInstall = [],
                            Install = [],
                            PostInstall  = [],
                            PreUninstall  = [],
                            Uninstall  = [],
                            PostUninstall  = [],
                        }
                    }
                ]
            };

            if (!await _context.UserInteraction.Confirm(
                "Do you want to create the following package manifest file?\n{0}",
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true })))
            {
                throw new OperationCanceledException("Operation canceled by the user.");
            }
        }

        // Create the manifest file path.
        string manifestPath = _pathManager.CurrentPackageManifestPath;

        // Check if the manifest file already exists.
        if (_context.FileSystem.File.Exists(manifestPath))
        {
            if (!force)
            {
                throw new InvalidOperationException($"The file '{manifestPath}' already exists. Use the -f or --force option to overwrite it.");
            }

            _context.Logger.LogWarning("The file '{ManifestPath}' already exists. Overwriting it.", manifestPath);
        }

        await _packageManager.SaveCurrentPackageManifest(manifest);
    }
}