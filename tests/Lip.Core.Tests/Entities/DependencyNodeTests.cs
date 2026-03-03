using Lip.Core.Entities;
using Semver;

namespace Lip.Core.Tests.Entities;

public class DependencyNodeTests {
  [Fact]
  public void Constructor_SetsPropertiesCorrectly() {
    // Arrange
    PackageId packageId = new("github.com/user/repo", string.Empty);
    SemVersion version = new(1, 0, 0);
    PackageSpec spec = new(packageId, version);
    List<PackageReqt> reqts =
    [
        new PackageReqt(new PackageId("github.com/other/dep", string.Empty), SemVersionRange.Parse("1.0.0"))
    ];

    // Act
    DependencyNode node = new(spec, reqts);

    // Assert
    Assert.Equal(spec, node.Spec);
    Assert.Same(reqts, node.Reqts);
  }
}