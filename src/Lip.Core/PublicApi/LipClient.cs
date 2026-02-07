using Flurl;
using Lip.Core.Entities;
using Lip.Core.Migration.PackageManifests;
using Lip.Core.Services;
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
    IRegistryService registryService,
    IWorkspaceService workspaceService) : ILipClient
{
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ICacheService _cacheService = cacheService;
    private readonly IConfigService _configService = configService;
    private readonly IInstallService _installService = installService;
    private readonly IRegistryService _registryService = registryService;
    private readonly IWorkspaceService _workspaceService = workspaceService;

    public async Task CacheClean()
    {
        await _cacheService.Clean();
    }

    public async Task ConfigDelete(string key)
    {
        await _configService.Delete(key);
    }

    public async Task<string> ConfigGet(string key)
    {
        return await _configService.Get(key);
    }

    public async Task<IDictionary<string, string>> ConfigList()
    {
        return await _configService.List();
    }

    public async Task ConfigSet(string key, string value)
    {
        await _configService.Set(key, value);
    }

    public async Task Init()
    {
        PackageManifest packageManifest = new()
        {
            Path = "github.com/user/repo",
            Version = new(0),
        };

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
        };

        using Stream stream = _fileSystem.File.Create("tooth.json");

        await JsonSerializer.SerializeAsync(stream, packageManifest, options);
    }

    public async Task Install(IEnumerable<string> packages, bool dryRun, bool ignoreScripts, bool noDependencies)
    {
        List<PackageSpec> parsedPackages = [];
        List<PackageId> flexiblePackages = [];
        List<IFileInfo> localPackages = [];
        List<Url> remotePackages = [];

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
                IFileInfo localPackage = _fileSystem.FileInfo.New(package) is { Exists: true } fileInfo
                    ? fileInfo
                    : throw new FileNotFoundException($"Local package '{package}' not found.");
                localPackages.Add(localPackage);
                continue;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                Url remotePackage = new(package);
                remotePackages.Add(remotePackage);
                continue;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            throw new AggregateException($"Failed to parse package '{package}'.", exceptions);
        }

        await _installService.InstallPackage(
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

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
        };

        using Stream outputStream = _fileSystem.File.Create(output);

        await JsonSerializer.SerializeAsync(outputStream, packageManifest, options);
    }

    public async Task Uninstall(IEnumerable<string> packages, bool dryRun, bool ignoreScripts, bool noDependencies)
    {
        IEnumerable<PackageId> parsedPackages = packages.Select(PackageId.Parse);

        await _installService.UninstallPackage(parsedPackages, dryRun, ignoreScripts, noDependencies);
    }

    public Task Update(IEnumerable<string> packages, bool dryRun, bool ignoreScripts)
    {
        IEnumerable<PackageSpec> parsedPackages = [];
        IEnumerable<PackageId> flexiblePackages = [];

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

            throw new AggregateException($"Failed to parse package '{package}'.", exceptions);
        }

        return _installService.UpdatePackage(parsedPackages, flexiblePackages, dryRun, ignoreScripts);
    }

    public async Task<string> View(string package)
    {
        PackageSpec packageSpec = PackageSpec.Parse(package);

        PackageManifest packageManifest = await _registryService.GetPackageManifest(packageSpec);

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
        };

        return JsonSerializer.Serialize(packageManifest, options);
    }
}