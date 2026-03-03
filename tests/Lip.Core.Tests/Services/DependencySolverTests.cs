using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Moq;
using Semver;

namespace Lip.Core.Tests.Services;

public class DependencySolverTests {
  private readonly Mock<IUserInteraction> _mockLogger;
  private readonly Mock<IPackageRegistry> _mockRegistry;
  private readonly DependencySolver _solver;

  public DependencySolverTests() {
    _mockLogger = new Mock<IUserInteraction>();
    _mockRegistry = new Mock<IPackageRegistry>();
    _solver = new DependencySolver(_mockRegistry.Object, _mockLogger.Object);
  }

  [Fact]
  public async Task Solve_NoDependencies_ReturnsEmpty() {
    // Act
    IEnumerable<PackageSpec> result = await _solver.Solve([]);

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public async Task Solve_SinglePackage_ReturnsPackage() {
    // Arrange
    PackageId packageId = new("github.com/test/pkg", string.Empty);
    SemVersion version = new(1, 0, 0);
    PackageSpec spec = new(packageId, version);
    PackageReqt reqt = new(packageId, SemVersionRange.All);

    _mockRegistry.Setup(r => r.GetAvailableVersions(packageId))
        .ReturnsAsync(new[] { version }.OrderBy(v => v));

    PackageManifest manifest = new() {
      Path = "github.com/test/pkg",
      Version = version,
      Variants =
        [
            new PackageManifestVariant
                {
                    Label = string.Empty,
                    Dependencies = []
                }
        ]
    };

    _mockRegistry.Setup(r => r.GetPackageManifest(spec))
        .ReturnsAsync(manifest);

    // Act
    IEnumerable<PackageSpec> result = await _solver.Solve([reqt]);

    // Assert
    Assert.Single(result);
    Assert.Equal(spec, result.First());
  }

  [Fact]
  public async Task Solve_Conflict_ThrowsException() {
    // Arrange
    PackageId pkgA = new("github.com/test/a", string.Empty);
    SemVersion verA = new(1, 0, 0);
    PackageSpec specA = new(pkgA, verA);

    PackageId pkgB = new("github.com/test/b", string.Empty);
    SemVersion verB1 = new(1, 0, 0);
    SemVersion verB2 = new(2, 0, 0);

    PackageReqt reqtA = new(pkgA, SemVersionRange.All);

    _mockRegistry.Setup(r => r.GetAvailableVersions(pkgA))
        .ReturnsAsync(new[] { verA }.OrderBy(v => v));

    PackageManifest manifestA = new() {
      Path = "github.com/test/a",
      Version = verA,
      Variants =
        [
            new PackageManifestVariant
                {
                    Label = string.Empty,
                    Dependencies = new Dictionary<PackageId, SemVersionRange>
                    {
                        { pkgB, SemVersionRange.Parse("1.0.0") }
                    }
                }
        ]
    };

    _mockRegistry.Setup(r => r.GetPackageManifest(specA))
        .ReturnsAsync(manifestA);

    // But we also request pkg-b 2.0.0 exactly at root level (contradiction)
    PackageReqt reqtB = new(pkgB, SemVersionRange.Parse("2.0.0"));

    _mockRegistry.Setup(r => r.GetAvailableVersions(pkgB))
        .ReturnsAsync(new[] { verB1, verB2 }.OrderBy(v => v));

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _solver.Solve([reqtA, reqtB]));
  }
}