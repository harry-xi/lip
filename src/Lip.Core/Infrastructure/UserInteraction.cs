namespace Lip.Core.Infrastructure;

public interface IUserInteraction
{
    Task PrintInfo(string message);
    Task PrintWarning(string message);
    Task PrintError(string message);
}