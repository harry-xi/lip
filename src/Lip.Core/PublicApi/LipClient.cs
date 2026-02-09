using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.Migration.PackageManifests;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Text.Json;

namespace Lip.Core.PublicApi;

public interface ILipClient
{
    Task CacheClean();
    Task ConfigDelete(string key);
    Task<string> ConfigGet(string key);
    Task<IDictionary<string, string>> ConfigList();
    Task ConfigSet(string key, string value);
    Task Init();
    Task Install(IEnumerable<string> packages, bool dryRun, bool ignoreScripts, bool noDependencies);
    Task<(IEnumerable<PackageSpec> ExplicitInstalled, IEnumerable<PackageSpec> ImplicitInstalled)> List();
    Task Migrate(string file, string output);
    Task Uninstall(IEnumerable<string> packages, bool dryRun, bool ignoreScripts, bool noDependencies);
    Task Update(IEnumerable<string> packages, bool dryRun, bool ignoreScripts);
    Task<string> View(string package);
}

public class LipClient(
    IFileSystem fileSystem,
    ICacheService cacheService,
    IConfigService configService,
    IInstallService installService,
    IPackageRegistry packageRegistry,
    IWorkspaceService workspaceService) : ILipClient
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ICacheService _cacheService = cacheService;
    private readonly IConfigService _configService = configService;
    private readonly IInstallService _installService = installService;
    private readonly IPackageRegistry _packageRegistry = packageRegistry;
    private readonly IWorkspaceService _workspaceService = workspaceService;

    public static async Task<LipClient> Create(ILogger logger)
    {
        FileSystem fileSystem = new();

        ConfigService configService = new(fileSystem, logger);

        CacheService cacheService = new(fileSystem);
        RuntimeConfig config = await configService.LoadConfig();
        GitRunner gitRunner = new();

        CommandRunner commandRunner = new();
        SourceService sourceService = new(
            gitRunner,
            cacheService,
            config.GithubProxy,
            config.GoModuleProxy);
        WorkspaceService workspaceService = new(fileSystem, logger);

        PackageInstaller packageInstaller = new(
            commandRunner,
            fileSystem,
            logger,
            sourceService,
            workspaceService);
        CompositePackageRegistry packageRegistry = new([
           new WorkspaceServicePackageRegistry(workspaceService),
           new GitPackageRegistry(gitRunner, config.GithubProxy),
           new GoModuleProxyPackageRegistry(config.GoModuleProxy),
           new LiprPackageRegistry(),
           new SourceServicePackageRegistry(sourceService),
        ]);

        InstallService installService = new(
            logger,
            packageInstaller,
            packageRegistry,
            sourceService,
            workspaceService);

        return new LipClient(
            fileSystem,
            cacheService,
            configService,
            installService,
            packageRegistry,
            workspaceService);
    }

    public async Task CacheClean()
    {
        await _cacheService.Clean();
    }

    public async Task ConfigDelete(string key)
    {
        RuntimeConfig config = await _configService.LoadConfig();

        RuntimeConfig newConfig = config.With(key, null);

        await _configService.SaveConfig(newConfig);
    }

    public async Task<string> ConfigGet(string key)
    {
        RuntimeConfig config = await _configService.LoadConfig();

        return config.AsDictionary()[key]?.ToString() ?? "";
    }

    public async Task<IDictionary<string, string>> ConfigList()
    {
        RuntimeConfig config = await _configService.LoadConfig();

        return config
            .AsDictionary()
            .ToDictionary<KeyValuePair<string, dynamic?>, string, string>(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? "");
    }

    public async Task ConfigSet(string key, string value)
    {
        RuntimeConfig config = await _configService.LoadConfig();

        RuntimeConfig newConfig = config.With(key, value);

        await _configService.SaveConfig(newConfig);
    }

    public async Task Init()
    {
        PackageManifest packageManifest = new()
        {
            Path = "github.com/user/repo",
            Version = new(0),
        };

        using Stream stream = _fileSystem.CreateFileWithDirectory("tooth.json");

        await JsonSerializer.SerializeAsync(stream, packageManifest, _jsonSerializerOptions);
    }

    public async Task Install(IEnumerable<string> packages, bool dryRun, bool ignoreScripts, bool noDependencies)
    {
        List<PackageSpec> parsedPackages = [];
        List<PackageId> flexiblePackages = [];
        List<LocalPackageSpec> localPackages = [];
        List<RemotePackageSpec> remotePackages = [];

        foreach (string package in packages)
        {
            // Order of parsing is:
            // 1. PackageSpec
            // 2. PackageId (flexible package)
            // 3. Local file
            // 4. Remote URL

            List<Exception> exceptions = [];

            try
            {
                PackageSpec packageSpec = PackageSpec.Parse(package);
                parsedPackages.Add(packageSpec);
                continue;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                PackageId packageId = PackageId.Parse(package);
                flexiblePackages.Add(packageId);
                continue;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                LocalPackageSpec localPackageSpec = LocalPackageSpec.Parse(package, _fileSystem);
                localPackages.Add(localPackageSpec);
                continue;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                RemotePackageSpec remotePackage = RemotePackageSpec.Parse(package);
                remotePackages.Add(remotePackage);
                continue;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            throw new AggregateException($"Failed to parse package '{package}'.", exceptions);
        }

        await _installService.InstallPackages(
            parsedPackages,
            flexiblePackages,
            localPackages,
            remotePackages,
            dryRun,
            ignoreScripts,
            noDependencies);
    }

    public async Task<(IEnumerable<PackageSpec> ExplicitInstalled, IEnumerable<PackageSpec> ImplicitInstalled)> List()
    {
        IEnumerable<PackageSpec> explicitInstalled = await _workspaceService
            .GetInstalledPackages(IWorkspaceService.PackageScope.Explicit);
        IEnumerable<PackageSpec> implicitInstalled = await _workspaceService
            .GetInstalledPackages(IWorkspaceService.PackageScope.Implicit);

        return (explicitInstalled, implicitInstalled);
    }

    public async Task Migrate(string file, string output)
    {
        using Stream inputStream = _fileSystem.File.OpenRead(file);

        JsonDocument inputJson = await JsonDocument.ParseAsync(inputStream);

        PackageManifest packageManifest = PackageManifestMigration.Migrate(inputJson);

        using Stream outputStream = _fileSystem.CreateFileWithDirectory(output);

        await JsonSerializer.SerializeAsync(outputStream, packageManifest, _jsonSerializerOptions);
    }

    public async Task Uninstall(IEnumerable<string> packages, bool dryRun, bool ignoreScripts, bool noDependencies)
    {
        IEnumerable<PackageId> parsedPackages = packages.Select(PackageId.Parse);

        await _installService.UninstallPackages(parsedPackages, dryRun, ignoreScripts, noDependencies);
    }

    public Task Update(IEnumerable<string> packages, bool dryRun, bool ignoreScripts)
    {
        IEnumerable<PackageSpec> parsedPackages = [];
        IEnumerable<PackageId> flexiblePackages = [];
        IEnumerable<LocalPackageSpec> localPackages = [];
        IEnumerable<RemotePackageSpec> remotePackages = [];

        foreach (string package in packages)
        {
            List<Exception> exceptions = [];

            try
            {
                PackageSpec packageSpec = PackageSpec.Parse(package);
                parsedPackages = parsedPackages.Append(packageSpec);
                continue;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                PackageId packageId = PackageId.Parse(package);
                flexiblePackages = flexiblePackages.Append(packageId);
                continue;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                LocalPackageSpec localPackageSpec = LocalPackageSpec.Parse(package, _fileSystem);
                localPackages = localPackages.Append(localPackageSpec);
                continue;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                RemotePackageSpec remotePackage = RemotePackageSpec.Parse(package);
                remotePackages = remotePackages.Append(remotePackage);
                continue;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            throw new AggregateException($"Failed to parse package '{package}'.", exceptions);
        }

        return _installService.UpdatePackages(
            parsedPackages,
            flexiblePackages,
            localPackages,
            remotePackages,
            dryRun,
            ignoreScripts);
    }

    public async Task<string> View(string package)
    {
        PackageSpec packageSpec = PackageSpec.Parse(package);

        PackageManifest packageManifest = await _packageRegistry.GetPackageManifest(packageSpec);

        return JsonSerializer.Serialize(packageManifest, _jsonSerializerOptions);
    }
}