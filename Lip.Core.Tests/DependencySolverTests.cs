using Microsoft.Extensions.Logging;
using Moq;
using Semver;
using System.Runtime.InteropServices;

namespace Lip.Core.Tests;

public class DependencySolverTests
{
    private readonly Mock<IContext> _mockContext;
    private readonly Mock<IPackageManager> _mockPackageManager;
    private readonly DependencySolver _solver;

    public DependencySolverTests()
    {
        _mockContext = new Mock<IContext>();
        _mockPackageManager = new Mock<IPackageManager>();

        // Mock logger
        _mockContext.Setup(c => c.Logger).Returns(new Mock<ILogger>().Object);

        _solver = new DependencySolver(_mockContext.Object, _mockPackageManager.Object);
    }

    private static (PackageManifest Manifest, PackageLock.Package Package) CreatePackage(string toothPath, string version, string variantLabel = "", Dictionary<string, string>? dependencies = null)
    {
        var deps = dependencies?.ToDictionary(
            kv => PackageIdentifier.Parse(kv.Key),
            kv => SemVersionRange.Parse(kv.Value)
        ) ?? [];

        var variant = new PackageManifest.Variant
        {
            Label = variantLabel,
            Platform = RuntimeInformation.RuntimeIdentifier,
            Dependencies = deps,
            Assets = [],
            PreserveFiles = [],
            RemoveFiles = [],
            Scripts = new PackageManifest.ScriptsType
            {
                PreInstall = [],
                Install = [],
                PostInstall = [],
                PrePack = [],
                PostPack = [],
                PreUninstall = [],
                Uninstall = [],
                PostUninstall = [],
                AdditionalScripts = []
            }
        };

        var manifest = new PackageManifest
        {
            ToothPath = toothPath,
            Version = SemVersion.Parse(version),
            Info = new() { Name = "", Description = "", Tags = [], AvatarUrl = new() },
            Variants = [variant]
        };

        var pkg = new PackageLock.Package
        {
            Files = [],
            Locked = false,
            Manifest = manifest,
            VariantLabel = variantLabel
        };

        return (manifest, pkg);
    }

    private void SetupManifest(string toothPath, string version, string variantLabel = "", Dictionary<string, string>? dependencies = null)
    {
        var (manifest, pkg) = CreatePackage(toothPath, version, variantLabel, dependencies);
        _mockPackageManager.Setup(pm => pm.GetPackageManifestFromCache(It.Is<PackageSpecifier>(s =>
            s.ToString() == pkg.Specifier.ToString())))
            .ReturnsAsync(manifest);
    }

    private void SetupRemoteVersions(string toothPath, string variantLabel, params string[] versions)
    {
        var id = PackageIdentifier.Parse(string.IsNullOrEmpty(variantLabel) ? toothPath : $"{toothPath}#{variantLabel}");
        _mockPackageManager.Setup(pm => pm.GetPackageRemoteVersions(id))
            .ReturnsAsync(versions.Select(v => SemVersion.Parse(v)).ToList());
    }

    private IEnumerable<(PackageIdentifier Identifier, SemVersionRange VersionRange)> ToRequirements(params PackageSpecifier[] specs)
    {
        return specs.Select(s => (s.Identifier, SemVersionRange.Parse(s.Version.ToString())));
    }

    [Fact]
    public async Task ResolveDependencies_NoDependencies_ReturnsInput()
    {
        // Arrange
        var pkgSpec = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0");
        SetupRemoteVersions("example.com/a", "", "1.0.0");

        // Act
        var result = await _solver.ResolveDependencies(ToRequirements(pkgSpec), []);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(pkgSpec, result[0]);
    }

    [Fact]
    public async Task ResolveDependencies_SimpleChain_ReturnsAll()
    {
        // Arrange
        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = "1.0.0" });
        SetupRemoteVersions("example.com/a", "", "1.0.0");

        SetupRemoteVersions("example.com/b", "", "1.0.0");
        SetupManifest("example.com/b", "1.0.0"); // B has no deps

        // Act
        var result = await _solver.ResolveDependencies(ToRequirements(pkgA), []);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.ToString() == "example.com/a@1.0.0");
        Assert.Contains(result, p => p.ToString() == "example.com/b@1.0.0");
    }

    [Fact]
    public async Task ResolveDependencies_DiamondDependency_ReturnsCorrectVersions()
    {
        // A -> B (1.0), C (1.0)
        // B -> D (1.0)
        // C -> D (1.0)
        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = "1.0.0", ["example.com/c"] = "1.0.0" });
        SetupRemoteVersions("example.com/a", "", "1.0.0");

        SetupRemoteVersions("example.com/b", "", "1.0.0");
        SetupManifest("example.com/b", "1.0.0", "", new() { ["example.com/d"] = "1.0.0" });

        SetupRemoteVersions("example.com/c", "", "1.0.0");
        SetupManifest("example.com/c", "1.0.0", "", new() { ["example.com/d"] = "1.0.0" });

        SetupRemoteVersions("example.com/d", "", "1.0.0");
        SetupManifest("example.com/d", "1.0.0");

        // Act
        var result = await _solver.ResolveDependencies(ToRequirements(pkgA), []);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.Contains(result, p => p.ToString() == "example.com/d@1.0.0");
    }

    [Fact]
    public async Task ResolveDependencies_VersionConflict_ThrowsException()
    {
        // A -> B (= 1.0.0)
        // C -> B (= 2.0.0)
        // Primary: A, C
        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        var pkgC = PackageSpecifier.Parse("example.com/c@1.0.0");

        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = "=1.0.0" });
        SetupManifest("example.com/c", "1.0.0", "", new() { ["example.com/b"] = "=2.0.0" });
        SetupRemoteVersions("example.com/a", "", "1.0.0");
        SetupRemoteVersions("example.com/c", "", "1.0.0");


        SetupRemoteVersions("example.com/b", "", "1.0.0", "2.0.0");
        SetupManifest("example.com/b", "1.0.0");
        SetupManifest("example.com/b", "2.0.0");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _solver.ResolveDependencies(ToRequirements(pkgA, pkgC), []));
    }

    [Fact]
    public async Task ResolveDependencies_WithKnownPackages_DoesNotFetchManifestForKnown()
    {
        // A -> B (1.0.0)
        // B is known/locked.
        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = "1.0.0" });
        SetupRemoteVersions("example.com/a", "", "1.0.0");

        var (_, knownB) = CreatePackage("example.com/b", "1.0.0");

        SetupRemoteVersions("example.com/b", "", "1.0.0");

        // Act
        var result = await _solver.ResolveDependencies(ToRequirements(pkgA), [knownB]);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, p => p.ToString() == "example.com/b@1.0.0");

        // Verify GetPackageManifestFromCache was NOT called for B
        _mockPackageManager.Verify(pm => pm.GetPackageManifestFromCache(It.Is<PackageSpecifier>(s => s.ToString() == "example.com/b@1.0.0")), Times.Never);
    }

    [Fact]
    public async Task ResolveDependencies_PicksVersionSatisfyingAllConstraints()
    {
        // A -> B (^1.0.0)
        // C -> B (^1.1.0)
        // Available B: 1.0.0, 1.1.0, 1.2.0
        // Should pick 1.1.0 or 1.2.0.

        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        var pkgC = PackageSpecifier.Parse("example.com/c@1.0.0");

        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = "^1.0.0" }); // >=1.0.0 <2.0.0
        SetupManifest("example.com/c", "1.0.0", "", new() { ["example.com/b"] = "^1.1.0" }); // >=1.1.0 <2.0.0
        SetupRemoteVersions("example.com/a", "", "1.0.0");
        SetupRemoteVersions("example.com/c", "", "1.0.0");

        SetupRemoteVersions("example.com/b", "", "1.0.0", "1.1.0", "1.2.0");
        SetupManifest("example.com/b", "1.0.0");
        SetupManifest("example.com/b", "1.1.0");
        SetupManifest("example.com/b", "1.2.0");

        // Act
        var result = await _solver.ResolveDependencies(ToRequirements(pkgA, pkgC), []);

        // Assert
        Assert.NotNull(result);
        var selectedB = result.Single(p => p.Identifier.ToothPath == "example.com/b");
        Assert.True(selectedB.Version.ComparePrecedenceTo(SemVersion.Parse("1.1.0")) >= 0);
    }

    [Fact]
    public async Task ResolveDependencies_RemoteFetchFails_UsesLocalIfAvailable()
    {
        // A -> B (1.0.0)
        // B is known locally. Remote fetch fails.
        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = "1.0.0" });
        SetupRemoteVersions("example.com/a", "", "1.0.0");

        var (_, knownB) = CreatePackage("example.com/b", "1.0.0");

        // Mock remote fetch failure for B, but success for A
        _mockPackageManager.Setup(pm => pm.GetPackageRemoteVersions(It.Is<PackageIdentifier>(id => id.ToothPath == "example.com/b")))
            .ThrowsAsync(new HttpRequestException("Network failure"));
        SetupRemoteVersions("example.com/a", "", "1.0.0"); // Re-setup A

        // Act
        var result = await _solver.ResolveDependencies(ToRequirements(pkgA), [knownB]);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, p => p.ToString() == "example.com/b@1.0.0");
    }

    [Fact]
    public async Task ResolveDependencies_VariantNotFound_ThrowsInvalidOperationException()
    {
        // A -> B (1.0.0)
        // B exists but variant matching runtime (default) not found/or manifest returns null variant for label.

        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = "1.0.0" });
        SetupRemoteVersions("example.com/a", "", "1.0.0");
        SetupRemoteVersions("example.com/b", "", "1.0.0");

        // Create a manifest for B that has NO variants for current runtime
        var manifestB = new PackageManifest
        {
            ToothPath = "example.com/b",
            Version = SemVersion.Parse("1.0.0"),
            Info = new() { Name = "", Description = "", Tags = [], AvatarUrl = new() },
            Variants = [] // Empty variants
        };

        _mockPackageManager.Setup(pm => pm.GetPackageManifestFromCache(It.Is<PackageSpecifier>(s => s.ToString() == "example.com/b@1.0.0")))
            .ReturnsAsync(manifestB);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _solver.ResolveDependencies(ToRequirements(pkgA), []));
    }

    [Fact]
    public async Task ResolveDependencies_UnsatisfiableDependency_ThrowsException()
    {
        // Hits Block 2: valid dependency exists but no version matches range.
        // A -> B (>= 2.0.0)
        // Available B: 1.0.0

        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = ">=2.0.0" });
        SetupRemoteVersions("example.com/a", "", "1.0.0");

        SetupRemoteVersions("example.com/b", "", "1.0.0");
        SetupManifest("example.com/b", "1.0.0");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _solver.ResolveDependencies(ToRequirements(pkgA), []));
    }

    [Fact]
    public async Task ResolveDependencies_ConflictWithCandidate_ThrowsException()
    {
        // Hits Block 1: Conflict with a package already in candidates list.
        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        var pkgC = PackageSpecifier.Parse("example.com/c@1.0.0");

        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = ">=1.0.0" });
        SetupManifest("example.com/c", "1.0.0", "", new() { ["example.com/b"] = "=2.0.0" });
        SetupRemoteVersions("example.com/a", "", "1.0.0");
        SetupRemoteVersions("example.com/c", "", "1.0.0");

        SetupRemoteVersions("example.com/b", "", "1.0.0", "1.1.0", "1.2.0");
        SetupManifest("example.com/b", "1.0.0");
        SetupManifest("example.com/b", "1.1.0");
        SetupManifest("example.com/b", "1.2.0");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _solver.ResolveDependencies(ToRequirements(pkgA, pkgC), []));
    }

    [Fact]
    public async Task ResolveDependencies_PrimaryPackageRange_SelectsBestVersion()
    {
        // Primary: A (>= 1.0.0)
        // Available A: 1.0.0, 2.0.0

        var idA = PackageIdentifier.Parse("example.com/a");
        SetupRemoteVersions("example.com/a", "", "1.0.0", "2.0.0");
        SetupManifest("example.com/a", "1.0.0");
        SetupManifest("example.com/a", "2.0.0");

        // Act
        var result = await _solver.ResolveDependencies(
            [(idA, SemVersionRange.Parse(">=1.0.0"))],
            []);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        // Assuming implementation sorts candidates Min, and then tries them.
        // It should pick one. Which one depends on sort order.
        // Versions are sorted by PrecedenceComparer.
        // The loop is foreach(version in sortedVersions).
        // If 1.0 < 2.0, and ascending, it tries 1.0 first.
        // So expected is 1.0.0.
        Assert.Equal("1.0.0", result[0].Version.ToString());
    }
    [Fact]
    public async Task ResolveDependencies_DuplicatePrimaryRequirement_ThrowsArgumentException()
    {
        // Arrange
        var idA = PackageIdentifier.Parse("example.com/a");
        var range1 = SemVersionRange.Parse(">=1.0.0");
        var range2 = SemVersionRange.Parse(">=2.0.0");

        SetupRemoteVersions("example.com/a", "", "1.0.0", "2.0.0");
        SetupManifest("example.com/a", "1.0.0");
        SetupManifest("example.com/a", "2.0.0");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _solver.ResolveDependencies(
            [(idA, range1), (idA, range2)],
            []));
    }

    [Fact]
    public async Task ResolveDependencies_PrimaryPackageNoCompatibleVersions_ReturnsNull()
    {
        // Arrange
        var idA = PackageIdentifier.Parse("example.com/a");
        var range = SemVersionRange.Parse(">=2.0.0");

        // Only version 1.0.0 is available
        SetupRemoteVersions("example.com/a", "", "1.0.0");
        SetupManifest("example.com/a", "1.0.0");

        // Act
        var result = await _solver.ResolveDependencies([(idA, range)], []);

        // Assert
        Assert.Null(result);

        // Verify error log
        // Note: verifying Logger calls on generic ILogger extension methods is tricky with Moq.
        // We verify that Log is called with LogLevel.Error.
        // The logger is mocked as _context.Logger which returns a Mock<ILogger>.
        // See constructor: _mockContext.Setup(c => c.Logger).Returns(new Mock<ILogger>().Object);
        // We need to access that mock to verify.

        Mock.Get(_mockContext.Object.Logger).Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}