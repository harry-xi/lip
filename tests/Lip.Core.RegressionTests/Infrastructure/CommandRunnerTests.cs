using Lip.Core.Infrastructure;

namespace Lip.Core.RegressionTests.Infrastructure;

public class CommandRunnerTests {
  [Fact]
  public async Task Run_SuccessfulCommand_DoesNotThrow() {
    CommandRunner runner = new();

    await runner.Run(OperatingSystem.IsWindows() ? "echo hello" : "echo hello");
  }
}