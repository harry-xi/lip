using Lip.Core.Infrastructure;
using Lip.Core.PublicApi;
using Moq;

namespace Lip.Core.RegressionTests.PublicApi;

public class LipClientTests {
  public enum WorkflowStepAction {
    Install,
    Uninstall
  }

  public record WorkflowStep(List<string> Packages, WorkflowStepAction Action);

  public static TheoryData<List<string>> Install_OnWinX64_DoesNotThrowData =>
  [
    ["github.com/LiteLDev/bds@1.26.3"],
    ["github.com/LiteLDev/LegacyScriptEngine@0.17.5"],
    ["github.com/LiteLDev/LeviLamina@1.9.7"],
    ["github.com/LiteLDev/LeviLamina@1.9.7", "github.com/LiteLDev/LegacyScriptEngine@0.17.5"],
    ["github.com/LiteLDev/LegacyScriptEngine@0.17.5", "github.com/LiteLDev/MoreDimensions@0.13.0"],
  ];

  [WinX64Theory]
  [MemberData(nameof(Install_OnWinX64_DoesNotThrowData))]
  public async Task Install_OnWinX64_DoesNotThrow(List<string> packages) {
    IUserInteraction userInteraction = CreateMockUserInteraction();

    string workspaceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(workspaceDir);

    Directory.SetCurrentDirectory(workspaceDir);

    LipClient lipClient = await LipClient.Create(userInteraction);

    await lipClient.Install(packages, dryRun: false, ignoreScripts: false, noDependencies: false);
  }

  public static TheoryData<List<WorkflowStep>> ComplexWorkflow_OnWinX64_DoesNotThrowData =>
  [
    [
      new(["github.com/LiteLDev/LeviLamina@1.9.7", "github.com/LiteLDev/LegacyScriptEngine@0.17.5"], WorkflowStepAction.Install),
      new(["github.com/LiteLDev/LegacyScriptEngine@0.17.5"], WorkflowStepAction.Uninstall),
      new(["github.com/LiteLDev/MoreDimensions@0.13.0"], WorkflowStepAction.Install),
      new(["github.com/LiteLDev/LegacyScriptEngine@0.17.5"], WorkflowStepAction.Install),
    ],
    [
      new(["github.com/LiteLDev/bds@1.26.3"], WorkflowStepAction.Install),
      new(["github.com/LiteLDev/LegacyScriptEngine@0.17.5"], WorkflowStepAction.Install),
      new(["github.com/LiteLDev/LeviLamina@1.9.7"], WorkflowStepAction.Install),
      new(["github.com/LiteLDev/LegacyScriptEngine@0.17.5"], WorkflowStepAction.Uninstall),
    ]
  ];

  [WinX64Theory]
  [MemberData(nameof(ComplexWorkflow_OnWinX64_DoesNotThrowData))]
  public async Task ComplexWorkflow_OnWinX64_DoesNotThrow(List<WorkflowStep> steps) {
    IUserInteraction userInteraction = CreateMockUserInteraction();

    string workspaceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(workspaceDir);

    Directory.SetCurrentDirectory(workspaceDir);

    LipClient lipClient = await LipClient.Create(userInteraction);

    foreach (var step in steps) {
      switch (step.Action) {
        case WorkflowStepAction.Install:
          await lipClient.Install(step.Packages, dryRun: false, ignoreScripts: false, noDependencies: false);
          break;

        case WorkflowStepAction.Uninstall:
          await lipClient.Uninstall(step.Packages, dryRun: false, ignoreScripts: false, noDependencies: false);
          break;

        default:
          throw new NotSupportedException($"Unsupported action: {step.Action}");
      }
    }
  }

  private static IUserInteraction CreateMockUserInteraction() {
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