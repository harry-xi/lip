namespace Lip.Core.Infrastructure;

public interface ICommandRunner
{
    Task Run(string command);
}