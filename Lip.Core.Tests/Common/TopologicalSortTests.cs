using Lip.Core;
using Semver;
using System.Linq;

namespace Lip.Core.Tests;

public class TopologicalSortTest
{
    [Fact]
    public void Sort_EmptyList_ReturnsEmpty()
    {
        // Arrange & Act.
        var result = TopologicalSort.Sort([]);

        // Assert.
        Assert.Empty(result);
    }

    [Fact]
    public void Sort_WithDependencies_ReturnsTopologicalOrder()
    {
        // Arrange.
        var pkg1 = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg1@1.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>
            {
                [PackageIdentifier.Parse("example.com/pkg2")] = SemVersionRange.Parse("2.0.0"),
            }.Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        var pkg2 = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg2@2.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>
            {
                [PackageIdentifier.Parse("example.com/pkg3")] = SemVersionRange.Parse("3.0.0"),
            }.Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        var pkg3 = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg3@3.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>().Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        // Act.
        var result = TopologicalSort.Sort([pkg1, pkg2, pkg3]);

        // Assert: pkg1 depends on pkg2 depends on pkg3, so order is [pkg1, pkg2, pkg3].
        Assert.Equal([pkg1, pkg2, pkg3], result);
    }

    [Fact]
    public void Sort_WithCycle_HandlesGracefully()
    {
        // Arrange: pkg1 -> pkg2 -> pkg3 -> pkg1 (cycle).
        var pkg1 = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg1@1.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>
            {
                [PackageIdentifier.Parse("example.com/pkg2")] = SemVersionRange.Parse("2.0.0"),
            }.Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        var pkg2 = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg2@2.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>
            {
                [PackageIdentifier.Parse("example.com/pkg3")] = SemVersionRange.Parse("3.0.0"),
            }.Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        var pkg3 = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg3@3.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>
            {
                [PackageIdentifier.Parse("example.com/pkg1")] = SemVersionRange.Parse("1.0.0"),
            }.Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        // Act: Should not throw, handles cycle gracefully.
        var result = TopologicalSort.Sort([pkg1, pkg2, pkg3]);

        // Assert: All items present.
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Sort_DuplicateIdentifiers_ThrowsInvalidOperationException()
    {
        // Arrange.
        var pkg1 = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg1@1.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>().Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        var pkg1Dupe = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg1@2.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>().Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        // Act & Assert.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            TopologicalSort.Sort([pkg1, pkg1Dupe]));
        Assert.Contains("example.com/pkg1", ex.Message);
    }

    [Fact]
    public void Sort_MissingDependency_IgnoresIt()
    {
        // Arrange: pkg1 depends on pkg2, but pkg2 is not in the list.
        var pkg1 = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg1@1.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>
            {
                [PackageIdentifier.Parse("example.com/pkg2")] = SemVersionRange.Parse("2.0.0"),
            }.Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        // Act.
        var result = TopologicalSort.Sort([pkg1]);

        // Assert.
        Assert.Single(result);
        Assert.Equal(pkg1, result[0]);
    }

    [Fact]
    public void Sort_VersionMismatch_IgnoresDependency()
    {
        // Arrange: pkg1 depends on pkg2@2.0.0, but pkg2 is 3.0.0.
        var pkg1 = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg1@1.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>
            {
                [PackageIdentifier.Parse("example.com/pkg2")] = SemVersionRange.Parse("2.0.0"),
            }.Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        var pkg2 = new PackageDependencyDescriptor(
            PackageSpecifier.Parse("example.com/pkg2@3.0.0"),
            new Dictionary<PackageIdentifier, SemVersionRange>().Select(kv => new PackageRequirement(kv.Key, kv.Value)));

        // Act.
        var result = TopologicalSort.Sort([pkg1, pkg2]);

        // Assert: Since version doesn't match, pkg2 is not treated as pkg1's dependency.
        Assert.Equal(2, result.Count);
    }
}