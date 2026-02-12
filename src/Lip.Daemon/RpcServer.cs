using Lip.Core.PublicApi;
using StreamJsonRpc;

namespace Lip.Daemon;

public interface IRpcServer
{
    Task Run();
}

public class RpcServer(ILipClient client) : IRpcServer
{
    private readonly ILipClient _client = client;

    public async Task Run()
    {
        using Stream stdin = Console.OpenStandardInput();
        using Stream stdout = Console.OpenStandardOutput();

        JsonRpc jsonRpc = new(stdout, stdin, _client);

        jsonRpc.StartListening();

        await jsonRpc.Completion;
    }
}
