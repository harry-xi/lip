using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Semver;
using Xunit;

namespace Lip.Core.Tests.Services;

public class DependencySolverTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IPackageRegistry> _mockRegistry;
    private readonly DependencySolver _solver;

    public DependencySolverTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockRegistry = new Mock<IPackageRegistry>();
        _solver = new DependencySolver(_mockLogger.Object, _mockRegistry.Object);
    }

    [Fact]
    public async Task Solve_NoDependencies_ReturnsEmpty()
    {
        // Act
        var result = await _solver.Solve([]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Solve_SinglePackage_ReturnsPackage()
    {
        // Arrange
        var packageId = new PackageId("github.com/test/pkg", string.Empty);
        var version = new SemVersion(1, 0, 0);
        var spec = new PackageSpec(packageId, version);
        var reqt = new PackageReqt(packageId, SemVersionRange.All);

        _mockRegistry.Setup(r => r.GetAvailableVersions(packageId))
            .ReturnsAsync([version]);

        var manifest = new PackageManifest
        {
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
        var result = await _solver.Solve([reqt]);

        // Assert
        Assert.Single(result);
        Assert.Equal(spec, result.First());
    }

    [Fact]
    public async Task Solve_Conflict_ThrowsException()
    {
        // Arrange
        var pkgA = new PackageId("github.com/test/a", string.Empty);
        var verA = new SemVersion(1, 0, 0);
        var specA = new PackageSpec(pkgA, verA);

        var pkgB = new PackageId("github.com/test/b", string.Empty);
        var verB1 = new SemVersion(1, 0, 0);
        var verB2 = new SemVersion(2, 0, 0);

        var reqtA = new PackageReqt(pkgA, SemVersionRange.All);

        _mockRegistry.Setup(r => r.GetAvailableVersions(pkgA))
            .ReturnsAsync([verA]);

        var manifestA = new PackageManifest
        {
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
        var reqtB = new PackageReqt(pkgB, SemVersionRange.Parse("2.0.0"));

        _mockRegistry.Setup(r => r.GetAvailableVersions(pkgB))
            .ReturnsAsync([verB1, verB2]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _solver.Solve([reqtA, reqtB]));
    }
}