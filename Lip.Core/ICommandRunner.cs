namespace Lip.Core;

public interface ICommandRunner
{
    Task<int> Run(string command, string workingDirectory);
}
