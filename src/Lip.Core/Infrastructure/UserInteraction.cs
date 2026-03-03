namespace Lip.Core.Infrastructure;

public interface IUserInteraction {
  Task PrintInfo(string message);
  Task PrintSuccess(string message);
  Task PrintWarning(string message);
  Task PrintError(string message);
  Task RunWithProgress(string message, Func<IProgress<double>, Task> action);
}