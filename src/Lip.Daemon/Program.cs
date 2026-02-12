using Lip.Core.PublicApi;
using Lip.Daemon;
using StreamJsonRpc;

using JsonRpc rpc = new(
    Console.OpenStandardInput(),
    Console.OpenStandardOutput());

IClientContract clientProxy = rpc.Attach<IClientContract>();

RpcUserInteraction userInteraction = new(clientProxy);

LipClient client = await LipClient.Create(userInteraction);

rpc.AddLocalRpcTarget(client);

rpc.StartListening();

await rpc.Completion;