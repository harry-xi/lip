using PolyType;
using StreamJsonRpc;

namespace Lip.Daemon;

[JsonRpcContract]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial interface IClientContract {
  Task PrintInfo(string message);
  Task PrintSuccess(string message);
  Task PrintWarning(string message);
  Task PrintError(string message);
  Task ReportProgress(string id, string message, double percentage);
}
