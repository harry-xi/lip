using Lip.Core.Services;

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

public class LipClient(ICacheService cacheService) : ILipClient
{
    private readonly ICacheService _cacheService = cacheService;

    public async Task CacheClean()
    {
        await _cacheService.Clean();
    }

    public Task ConfigDelete(string key)
    {
        throw new NotImplementedException();
    }

    public Task<string> ConfigGet(string key)
    {
        throw new NotImplementedException();
    }

    public Task<IDictionary<string, string>> ConfigList()
    {
        throw new NotImplementedException();
    }

    public Task ConfigSet(string key, string value)
    {
        throw new NotImplementedException();
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

    public Task<string> View(string package)
    {
        throw new NotImplementedException();
    }
}