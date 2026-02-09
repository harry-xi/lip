using Lip.Core.Entities;
using Semver;

namespace Lip.Core.Tests.Entities;

public class DependencyNodeTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var packageId = new PackageId("github.com/user/repo", string.Empty);
        var version = new SemVersion(1, 0, 0);
        var spec = new PackageSpec(packageId, version);
        var reqts = new List<PackageReqt>
        {
            new PackageReqt(new PackageId("github.com/other/dep", string.Empty), SemVersionRange.Parse("1.0.0"))
        };

        // Act
        var node = new DependencyNode(spec, reqts);

        // Assert
        Assert.Equal(spec, node.Spec);
        Assert.Same(reqts, node.Reqts);
    }
}