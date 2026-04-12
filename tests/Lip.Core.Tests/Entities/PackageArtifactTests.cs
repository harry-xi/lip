using Lip.Core.Entities;
using Lip.Core.Sources;
using Moq;
using Semver;

namespace Lip.Core.Tests.Entities;

public class PackageArtifactTests {
  [Fact]
  public void Constructor_SetsPropertiesCorrectly() {
    // Arrange
    PackageId packageId = new("github.com/user/repo", string.Empty);
    SemVersion version = new(1, 0, 0);
    PackageSpec spec = new(packageId, version);
    Mock<ISource> mockSource = new();

    // Act
    PackageArtifact artifact = new(spec, mockSource.Object);

    // Assert
    Assert.Same(spec, artifact.Spec);
    Assert.Same(mockSource.Object, artifact.Source);
  }
}
