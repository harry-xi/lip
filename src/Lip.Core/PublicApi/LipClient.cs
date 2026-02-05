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
