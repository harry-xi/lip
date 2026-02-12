using Lip.Core.PublicApi;
using StreamJsonRpc;

namespace Lip.Daemon;

public interface IRpcServer
{
    Task Run();
}

public class RpcServer(ILipClient client, Stream @in, Stream @out) : IRpcServer
{
    private readonly ILipClient _client = client;

    public async Task Run()
    {
        using JsonRpc rpc = JsonRpc.Attach(
            sendingStream: @out,
            receivingStream: @in,
            target: _client);

        await rpc.Completion;
    }
}
