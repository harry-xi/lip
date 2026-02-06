using Lip.Core.Entities;
using Lip.Core.Registries;
using Lip.Core.Services;
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
    ICacheService cacheService,
    IConfigService configService,
    IRegistryService registryService) : ILipClient
{
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

    public Task Init()
    {
        throw new NotImplementedException();
    }

    public Task Install(IEnumerable<string> packages, bool dryRun, bool ignoreScripts, bool noDependencies)
    {
        throw new NotImplementedException();
    }

    public Task<string> List()
    {
        throw new NotImplementedException();
    }

    public Task Migrate(string file, string output)
    {
        throw new NotImplementedException();
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