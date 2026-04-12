using System.Runtime.InteropServices;

namespace Lip.Core.RegressionTests;

public sealed class WinX64TheoryAttribute : TheoryAttribute {
  public WinX64TheoryAttribute() {
    bool isWinX64 =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
        RuntimeInformation.OSArchitecture == Architecture.X64;

    if (!isWinX64) {
      Skip = "Regression tests run only on win-x64.";
    }
  }
}
