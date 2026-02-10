namespace Lip.Core.Infrastructure;

public interface IUserInteraction
{
    Task PrintError(string message);
    Task PrintInfo(string message);
    Task PrintSuccess(string message);
    Task PrintWarning(string message);
    Task PrintTable(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows);
    Task PrintList(string header, IEnumerable<string> items);
}