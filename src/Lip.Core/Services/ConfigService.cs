namespace Lip.Core.Services;

public interface IConfigService
{
    Task Delete(string key);
    Task<string> Get(string key);
    Task<IDictionary<string, string>> List();
    Task Set(string key, string value);
}