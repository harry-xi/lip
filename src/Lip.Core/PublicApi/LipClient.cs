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
    Task Install(
        IEnumerable<string> packages,
        bool dryRun,
        bool ignoreScripts,
        bool noDependencies);
    Task<string> List();
    Task Migrate(string file, string output);
    Task Uninstall(
        IEnumerable<string> packages,
        bool dryRun,
        bool ignoreScripts,
        bool noDependencies
    );
    Task Update(
        IEnumerable<string> packages,
        bool dryRun,
        bool ignoreScripts,
        bool noDependencies
    );
    Task<string> View(string package);
}

public class LipClient(
    IFileSystem fileSystem,
    ICacheService cacheService,
    IConfigService configService,
    IRegistryService registryService) : ILipClient
{
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ICacheService _cacheService = cacheService;
    private readonly IConfigService _configService = configService;
    private readonly IRegistryService _registryService = registryService;

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

    public Task Install(IEnumerable<string> packages, bool dryRun, bool ignoreScripts, bool noDependencies)
    {
        throw new NotImplementedException();
    }

    public Task<string> List()
    {
        throw new NotImplementedException();
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

    public Task Uninstall(IEnumerable<string> packages, bool dryRun, bool ignoreScripts, bool noDependencies)
    {
        throw new NotImplementedException();
    }

    public Task Update(IEnumerable<string> packages, bool dryRun, bool ignoreScripts, bool noDependencies)
    {
        throw new NotImplementedException();
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