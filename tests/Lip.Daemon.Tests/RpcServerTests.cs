using Lip.Core.PublicApi;
using System.Text;
using System.Text.Json;
using Moq;

namespace Lip.Daemon.Tests;

public class RpcServerTests
{
    private readonly Mock<ILipClient> _mockClient = new();
}
