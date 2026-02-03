using Lip.Core.Tests;

namespace Lip.Core.Tests;

public partial class PackageManifestTests
{
    [Fact]
    public void ScriptsType_Constructor_ValidValues_ReturnsCorrectInstance()
    {
        // Arrange & Act.


        PackageManifest.ScriptsType scripts = new()
        {
            PreInstall = [],
            Install = [],
            PostInstall = [],
            PreUninstall = [],
            Uninstall = [],
            PostUninstall = [],

        };

        PackageManifest.ScriptsType newScripts = scripts with { };

        // Assert.
        Assert.Empty(newScripts.PreInstall);
        Assert.Empty(newScripts.Install);
        Assert.Empty(newScripts.PostInstall);
        Assert.Empty(newScripts.PreUninstall);
        Assert.Empty(newScripts.Uninstall);
        Assert.Empty(newScripts.PostUninstall);
    }
}