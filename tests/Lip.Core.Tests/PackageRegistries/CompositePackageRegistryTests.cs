using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Moq;
using Semver;

namespace Lip.Core.Tests.PackageRegistries;

public class CompositePackageRegistryTests
{
    [Fact]
    public async Task GetAvailableVersions_AggregatesFromAllRegistries()
    {
        // Arrange
        PackageId pkgId = new("github.com/test/pkg", "");
        Mock<IPackageRegistry> mockRegistry1 = new();
        Mock<IPackageRegistry> mockRegistry2 = new();

        mockRegistry1.Setup(r => r.GetAvailableVersions(pkgId))
            .ReturnsAsync(new[] { new SemVersion(1, 0, 0), new SemVersion(1, 1, 0) }.Order(SemVersion.PrecedenceComparer));
        mockRegistry2.Setup(r => r.GetAvailableVersions(pkgId))
            .ReturnsAsync(new[] { new SemVersion(1, 1, 0), new SemVersion(2, 0, 0) }.Order(SemVersion.PrecedenceComparer));

        CompositePackageRegistry composite = new([mockRegistry1.Object, mockRegistry2.Object]);

        // Act
        List<SemVersion> result = (await composite.GetAvailableVersions(pkgId)).ToList();

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
        PackageId pkgId = new("github.com/test/pkg", "");
        Mock<IPackageRegistry> mockRegistry1 = new();
        Mock<IPackageRegistry> mockRegistry2 = new();

        mockRegistry1.Setup(r => r.GetAvailableVersions(pkgId))
            .ThrowsAsync(new Exception("Registry 1 failed"));
        mockRegistry2.Setup(r => r.GetAvailableVersions(pkgId))
            .ReturnsAsync(new[] { new SemVersion(1, 0, 0) }.OrderBy(v => v, SemVersion.PrecedenceComparer));

        CompositePackageRegistry composite = new([mockRegistry1.Object, mockRegistry2.Object]);

        // Act
        List<SemVersion> result = (await composite.GetAvailableVersions(pkgId)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(new SemVersion(1, 0, 0), result[0]);
    }

    [Fact]
    public async Task GetAvailableVersions_AllRegistriesFail_ThrowsAggregateException()
    {
        // Arrange
        PackageId pkgId = new("github.com/test/pkg", "");
        Mock<IPackageRegistry> mockRegistry1 = new();
        Mock<IPackageRegistry> mockRegistry2 = new();

        mockRegistry1.Setup(r => r.GetAvailableVersions(pkgId))
            .ThrowsAsync(new Exception("Registry 1 failed"));
        mockRegistry2.Setup(r => r.GetAvailableVersions(pkgId))
            .ThrowsAsync(new Exception("Registry 2 failed"));

        CompositePackageRegistry composite = new([mockRegistry1.Object, mockRegistry2.Object]);

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => composite.GetAvailableVersions(pkgId));
    }

    [Fact]
    public async Task GetPackageManifest_FirstRegistrySucceeds_ReturnsManifest()
    {
        // Arrange
        PackageSpec pkgSpec = new(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));
        PackageManifest manifest = new() { Path = "github.com/test/pkg", Version = new SemVersion(1, 0, 0) };

        Mock<IPackageRegistry> mockRegistry1 = new();
        Mock<IPackageRegistry> mockRegistry2 = new();

        mockRegistry1.Setup(r => r.GetPackageManifest(pkgSpec)).ReturnsAsync(manifest);

        CompositePackageRegistry composite = new([mockRegistry1.Object, mockRegistry2.Object]);

        // Act
        PackageManifest result = await composite.GetPackageManifest(pkgSpec);

        // Assert
        Assert.Equal(manifest, result);
        mockRegistry2.Verify(r => r.GetPackageManifest(It.IsAny<PackageSpec>()), Times.Never);
    }

    [Fact]
    public async Task GetPackageManifest_AllFail_ThrowsAggregateException()
    {
        // Arrange
        PackageSpec pkgSpec = new(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));

        Mock<IPackageRegistry> mockRegistry1 = new();
        Mock<IPackageRegistry> mockRegistry2 = new();

        mockRegistry1.Setup(r => r.GetPackageManifest(pkgSpec))
            .ThrowsAsync(new Exception("Failed 1"));
        mockRegistry2.Setup(r => r.GetPackageManifest(pkgSpec))
            .ThrowsAsync(new Exception("Failed 2"));

        CompositePackageRegistry composite = new([mockRegistry1.Object, mockRegistry2.Object]);

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => composite.GetPackageManifest(pkgSpec));
    }

    [Fact]
    public async Task GetPackageManifest_EmptyRegistries_ThrowsInvalidOperationExceptionOrAggregateException()
    {
        // Note: Currently CompositePackageRegistry probably throws or returns null if list is empty?
        // Looking at implementation logic (implied from typical usage), if empty, it probably throws AggregateException or similar because it tries nothing.

        PackageSpec pkgSpec = new(new PackageId("github.com/test/pkg", ""), new SemVersion(1, 0, 0));
        CompositePackageRegistry composite = new([]);

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