using Lip.Core.Infrastructure;
using Lip.Core.PublicApi;
using Moq;

namespace Lip.Core.RegressionTests.PublicApi;

public class LipClientTests {
  public static TheoryData<string[]> Install_OnWinX64_DoesNotThrowData =>
  [
      ["github.com/LiteLDev/bds@1.26.3"],
        ["github.com/LiteLDev/LegacyScriptEngine@0.17.5"],
        ["github.com/LiteLDev/LeviLamina@1.9.7"],
        ["github.com/LiteLDev/LeviLamina@1.9.7", "github.com/LiteLDev/LegacyScriptEngine@0.17.5"],
        ["github.com/LiteLDev/LegacyScriptEngine@0.17.5", "github.com/LiteLDev/MoreDimensions@0.13.0"],
    ];

  [WinX64Theory]
  [MemberData(nameof(Install_OnWinX64_DoesNotThrowData))]
  public async Task Install_OnWinX64_DoesNotThrow(string[] packages) {

    IUserInteraction userInteraction = CreateNoOpUserInteraction();

    string workspaceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(workspaceDir);

    Directory.SetCurrentDirectory(workspaceDir);

    LipClient lipClient = await LipClient.Create(userInteraction);

    await lipClient.Install(packages, dryRun: false, ignoreScripts: false, noDependencies: false);
  }

  private static IUserInteraction CreateNoOpUserInteraction() {
    Mock<IUserInteraction> mock = new();

    mock.Setup(u => u.PrintInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
    mock.Setup(u => u.PrintWarning(It.IsAny<string>())).Returns(Task.CompletedTask);
    mock.Setup(u => u.PrintError(It.IsAny<string>())).Returns(Task.CompletedTask);
    mock.Setup(u => u.RunWithProgress(It.IsAny<string>(), It.IsAny<Func<IProgress<double>, Task>>()))
        .Returns((string _, Func<IProgress<double>, Task> action) =>
            action(new Progress<double>()));

    return mock.Object;
  }
}