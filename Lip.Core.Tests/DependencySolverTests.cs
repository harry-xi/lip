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

    [Fact]
    public async Task ResolveDependencies_NoDependencies_ReturnsInput()
    {
        // Arrange
        var pkgSpec = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0");

        // Act
        var result = await _solver.ResolveDependencies([pkgSpec], []);

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

        SetupRemoteVersions("example.com/b", "", "1.0.0");
        SetupManifest("example.com/b", "1.0.0"); // B has no deps

        // Act
        var result = await _solver.ResolveDependencies([pkgA], []);

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

        SetupRemoteVersions("example.com/b", "", "1.0.0");
        SetupManifest("example.com/b", "1.0.0", "", new() { ["example.com/d"] = "1.0.0" });

        SetupRemoteVersions("example.com/c", "", "1.0.0");
        SetupManifest("example.com/c", "1.0.0", "", new() { ["example.com/d"] = "1.0.0" });

        SetupRemoteVersions("example.com/d", "", "1.0.0");
        SetupManifest("example.com/d", "1.0.0");

        // Act
        var result = await _solver.ResolveDependencies([pkgA], []);

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

        SetupRemoteVersions("example.com/b", "", "1.0.0", "2.0.0");
        SetupManifest("example.com/b", "1.0.0");
        SetupManifest("example.com/b", "2.0.0");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _solver.ResolveDependencies([pkgA, pkgC], []));
    }

    [Fact]
    public async Task ResolveDependencies_WithKnownPackages_DoesNotFetchManifestForKnown()
    {
        // A -> B (1.0.0)
        // B is known/locked.
        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = "1.0.0" });

        var (_, knownB) = CreatePackage("example.com/b", "1.0.0");

        SetupRemoteVersions("example.com/b", "", "1.0.0");

        // Act
        var result = await _solver.ResolveDependencies([pkgA], [knownB]);

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
        // Since solver sorts ascending, it might pick 1.1.0 if both satisfy.

        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        var pkgC = PackageSpecifier.Parse("example.com/c@1.0.0");

        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = "^1.0.0" }); // >=1.0.0 <2.0.0
        SetupManifest("example.com/c", "1.0.0", "", new() { ["example.com/b"] = "^1.1.0" }); // >=1.1.0 <2.0.0

        SetupRemoteVersions("example.com/b", "", "1.0.0", "1.1.0", "1.2.0");
        SetupManifest("example.com/b", "1.0.0");
        SetupManifest("example.com/b", "1.1.0");
        SetupManifest("example.com/b", "1.2.0");

        // Act
        var result = await _solver.ResolveDependencies([pkgA, pkgC], []);

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

        var (_, knownB) = CreatePackage("example.com/b", "1.0.0");

        // Mock remote fetch failure
        _mockPackageManager.Setup(pm => pm.GetPackageRemoteVersions(It.IsAny<PackageIdentifier>()))
            .ThrowsAsync(new HttpRequestException("Network failure"));

        // Act
        var result = await _solver.ResolveDependencies([pkgA], [knownB]);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, p => p.ToString() == "example.com/b@1.0.0");
    }

    [Fact]
    public async Task ResolveDependencies_VariantNotFound_ThrowsInvalidOperationException()
    {
        // A -> B (1.0.0)
        // B exists but variant matching runtime (default) not found/or manifest returns null variant for label.
        // Since our CreatePackage creates a valid variant, we need to mock GetPackageManifestFromCache to return a manifest 
        // that DOES NOT have the matching variant.

        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = "1.0.0" });
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
        await Assert.ThrowsAsync<InvalidOperationException>(() => _solver.ResolveDependencies([pkgA], []));
    }

    [Fact]
    public async Task ResolveDependencies_UnsatisfiableDependency_ThrowsException()
    {
        // Hits Block 2: valid dependency exists but no version matches range.
        // A -> B (>= 2.0.0)
        // Available B: 1.0.0

        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = ">=2.0.0" });

        SetupRemoteVersions("example.com/b", "", "1.0.0");
        SetupManifest("example.com/b", "1.0.0");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _solver.ResolveDependencies([pkgA], []));
    }

    [Fact]
    public async Task ResolveDependencies_ConflictWithCandidate_ThrowsException()
    {
        // Hits Block 1: Conflict with a package already in candidates list.
        // We ensure B is in candidates but NOT selected yet when C is processed.
        // A -> B (>= 1.0.0). Available B: 1.0.0, 1.1.0, 1.2.0. -> Candidates B has 3 options.
        // C -> B (= 2.0.0).
        // Processing order:
        // 1. Pick A (1 version). B added to candidates (3 versions).
        //    Candidates: {C (1 version), B (3 versions)}.
        // 2. Pick C (fewest versions).
        //    C deps on B(=2.0.0). B is in candidates.
        //    Intersection({1.0, 1.1, 1.2}, =2.0.0) is empty. -> Block 1.

        var pkgA = PackageSpecifier.Parse("example.com/a@1.0.0");
        var pkgC = PackageSpecifier.Parse("example.com/c@1.0.0");

        SetupManifest("example.com/a", "1.0.0", "", new() { ["example.com/b"] = ">=1.0.0" });
        SetupManifest("example.com/c", "1.0.0", "", new() { ["example.com/b"] = "=2.0.0" });

        SetupRemoteVersions("example.com/b", "", "1.0.0", "1.1.0", "1.2.0");
        SetupManifest("example.com/b", "1.0.0");
        SetupManifest("example.com/b", "1.1.0");
        SetupManifest("example.com/b", "1.2.0");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _solver.ResolveDependencies([pkgA, pkgC], []));
    }
}