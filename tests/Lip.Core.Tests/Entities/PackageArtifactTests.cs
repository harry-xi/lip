using Lip.Core.Entities;
using Lip.Core.SourceProviders;
using Moq;
using Semver;

namespace Lip.Core.Tests.Entities;

public class PackageArtifactTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var packageId = new PackageId("github.com/user/repo", string.Empty);
        var version = new SemVersion(1, 0, 0);
        var spec = new PackageSpec(packageId, version);
        var mockSourceProvider = new Mock<ISourceProvider>();

        // Act
        var artifact = new PackageArtifact(spec, mockSourceProvider.Object);

        // Assert
        Assert.Same(spec, artifact.Spec);
        Assert.Same(mockSourceProvider.Object, artifact.SourceProvider);
    }
}