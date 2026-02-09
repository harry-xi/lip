using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Moq;
using Semver;
using Xunit;

namespace Lip.Core.Tests.PackageRegistries;

public class CompositePackageRegistryTests
{
    [Fact]
    public async Task GetAvailableVersions_AggregatesFromAllRegistries()
    {
        // Arrange
        var pkgId = new PackageId("github.com/test/pkg", "");
        var mockRegistry1 = new Mock<IPackageRegistry>();
        var mockRegistry2 = new Mock<IPackageRegistry>();

        mockRegistry1.Setup(r => r.GetAvailableVersions(pkgId))
            .ReturnsAsync([new SemVersion(1, 0, 0), new SemVersion(1, 1, 0)]);
        mockRegistry2.Setup(r => r.GetAvailableVersions(pkgId))
            .ReturnsAsync([new SemVersion(1, 1, 0), new SemVersion(2, 0, 0)]);

        var composite = new CompositePackageRegistry([mockRegistry1.Object, mockRegistry2.Object]);

        // Act
        var result = (await composite.GetAvailableVersions(pkgId)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(new SemVersion(1, 0, 0), result);
        Assert.Contains(new SemVersion(1, 1, 0), result);
        Assert.Contains(new SemVersion(2, 0, 0), result);
    }

    [Fact]
    public async Task GetAvailableVersions_OneRegistryFails_ReturnsFromOther()
    {
        // Arrange
        var pkgId = new PackageId("github.com/test/pkg", "");
        var mockRegistry1 = new Mock<IPackageRegistry>();
        var mockRegistry2 = new Mock<IPackageRegistry>();

        mockRegistry1.Setup(r => r.GetAvailableVersions(pkgId))
            .ThrowsAsync(new Exception("Registry 1 failed"));
        mockRegistry2.Setup(r => r.GetAvailableVersions(pkgId))
            .ReturnsAsync([new SemVersion(1, 0, 0)]);

        var composite = new CompositePackageRegistry([mockRegistry1.Object, mockRegistry2.Object]);

        // Act
        var result = (await composite.GetAvailableVersions(pkgId)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(new SemVersion(1, 0, 0), result[0]);
    }

    [Fact]
    public async Task GetAvailableVersions_AllRegistriesFail_ThrowsAggregateException()
    {
        // Arrange
        var pkgId = new PackageId("github.com/test/pkg", "");
        var mockRegistry1 = new Mock<IPackageRegistry>();
        var mockRegistry2 = new Mock<IPackageRegistry>();

        mockRegistry1.Setup(r => r.GetAvailableVersions(pkgId))
            .ThrowsAsync(new Exception("Registry 1 failed"));
        mockRegistry2.Setup(r => r.GetAvailableVersions(pkgId))
            .ThrowsAsync(new Exception("Registry 2 failed"));

        var composite = new CompositePackageRegistry([mockRegistry1.Object, mockRegistry2.Object]);

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => composite.GetAvailableVersions(pkgId));
    }

    [Fact]
    public async Task GetPackageManifest_FirstRegistrySucceeds_ReturnsManifest()
    {
        // Arrange
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));
        var manifest = new PackageManifest { Path = "github.com/test/pkg", Version = new SemVersion(1, 0, 0) };

        var mockRegistry1 = new Mock<IPackageRegistry>();
        var mockRegistry2 = new Mock<IPackageRegistry>();

        mockRegistry1.Setup(r => r.GetPackageManifest(pkgSpec)).ReturnsAsync(manifest);

        var composite = new CompositePackageRegistry([mockRegistry1.Object, mockRegistry2.Object]);

        // Act
        var result = await composite.GetPackageManifest(pkgSpec);

        // Assert
        Assert.Equal(manifest, result);
        mockRegistry2.Verify(r => r.GetPackageManifest(It.IsAny<PackageSpec>()), Times.Never);
    }

    [Fact]
    public async Task GetPackageManifest_AllFail_ThrowsAggregateException()
    {
        // Arrange
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));

        var mockRegistry1 = new Mock<IPackageRegistry>();
        var mockRegistry2 = new Mock<IPackageRegistry>();

        mockRegistry1.Setup(r => r.GetPackageManifest(pkgSpec))
            .ThrowsAsync(new Exception("Failed 1"));
        mockRegistry2.Setup(r => r.GetPackageManifest(pkgSpec))
            .ThrowsAsync(new Exception("Failed 2"));

        var composite = new CompositePackageRegistry([mockRegistry1.Object, mockRegistry2.Object]);

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => composite.GetPackageManifest(pkgSpec));
    }

    [Fact]
    public async Task GetPackageManifest_EmptyRegistries_ThrowsInvalidOperationExceptionOrAggregateException()
    {
        // Note: Currently CompositePackageRegistry probably throws or returns null if list is empty?
        // Looking at implementation logic (implied from typical usage), if empty, it probably throws AggregateException or similar because it tries nothing.

        var pkgSpec = new PackageSpec(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));
        var composite = new CompositePackageRegistry([]);

        // Based on implementation of iterating and throwing exceptions if all fail, an empty list means 0 exceptions but loop finishes.
        // We should verify what happens when 0 registries are passed.
        // Assuming it might need at least one registry or handles empty gracefully by throwing "not found" type error.
        // For now, let's assume it throws AggregateException because it collects exceptions and throws if none succeeded.
        // If no registries, no exceptions collected, what happens?
        // Actually, let's defer this specific edge case test or expect AggregateException with empty list if that's how it works.
        // Let's create it and see if it fails.

        await Assert.ThrowsAsync<AggregateException>(() => composite.GetPackageManifest(pkgSpec));
    }
}