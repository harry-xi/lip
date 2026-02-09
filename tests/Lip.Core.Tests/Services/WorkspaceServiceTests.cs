using Lip.Core.Entities;
using Lip.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Semver;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Lip.Core.Tests.Services;

public class WorkspaceServiceTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly Mock<ILogger> _logger;
    private readonly WorkspaceService _service;

    public WorkspaceServiceTests()
    {
        _fileSystem = new MockFileSystem();
        _logger = new Mock<ILogger>();
        _service = new WorkspaceService(_fileSystem, _logger.Object);
    }

    [Fact]
    public async Task AddInstalledPackage_AddsPackageToState()
    {
        var pkgId = new PackageId("github.com/foo/bar", "");
        var version = new SemVersion(1, 0, 0);
        var pkgSpec = new PackageSpec(pkgId, version);
        var manifest = new PackageManifest { Path = "github.com/foo/bar", Version = version, Variants = [new()] };
        var files = new List<string> { @"C:\lip\plugins\foo.dll" };
        var fileInfoList = files.Select(f => _fileSystem.FileInfo.New(f)).ToList();

        await _service.AddInstalledPackage(pkgSpec, manifest, fileInfoList, true);

        var installed = await _service.GetInstalledPackages(IWorkspaceService.PackageScope.All);
        Assert.Single(installed);
        Assert.Equal(pkgSpec, installed.First());
    }

    [Fact]
    public async Task AddInstalledPackage_MismatchedSpec_ThrowsArgumentException()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));
        var manifest = new PackageManifest { Path = "github.com/other/repo", Version = new SemVersion(2, 0, 0), Variants = [new()] };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddInstalledPackage(pkgSpec, manifest, [], true));
    }

    [Fact]
    public async Task AddInstalledPackage_AlreadyInstalled_ThrowsInvalidOperationException()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));
        var manifest = new PackageManifest { Path = "github.com/test/repo", Version = new SemVersion(1, 0, 0), Variants = [new()] };
        await _service.AddInstalledPackage(pkgSpec, manifest, [], true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddInstalledPackage(pkgSpec, manifest, [], true));
    }

    [Fact]
    public async Task GetInstalledPackageFiles_ReturnsFiles()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));
        var manifest = new PackageManifest { Path = "github.com/test/repo", Version = new SemVersion(1, 0, 0), Variants = [new()] };
        _fileSystem.AddFile(@"C:\test\file.dll", new MockFileData("content"));
        var files = new[] { _fileSystem.FileInfo.New(@"C:\test\file.dll") };
        await _service.AddInstalledPackage(pkgSpec, manifest, files, true);

        var result = await _service.GetInstalledPackageFiles(pkgSpec);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetInstalledPackageFiles_NotInstalled_ThrowsInvalidOperationException()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetInstalledPackageFiles(pkgSpec));
    }

    [Fact]
    public async Task GetInstalledPackageManifest_ReturnsCorrectManifest()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));
        var manifest = new PackageManifest { Path = "github.com/test/repo", Version = new SemVersion(1, 0, 0), Variants = [new()] };
        await _service.AddInstalledPackage(pkgSpec, manifest, [], true);

        var result = await _service.GetInstalledPackageManifest(pkgSpec);

        Assert.Equal(manifest.Path, result.Path);
    }

    [Fact]
    public async Task GetInstalledPackageManifest_NotInstalled_ThrowsInvalidOperationException()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetInstalledPackageManifest(pkgSpec));
    }

    [Fact]
    public async Task GetInstalledPackages_ExplicitScope_ReturnsOnlyExplicit()
    {
        var explicitPkg = new PackageSpec(new PackageId("github.com/explicit/pkg", ""), new SemVersion(1, 0, 0));
        var implicitPkg = new PackageSpec(new PackageId("github.com/implicit/pkg", ""), new SemVersion(1, 0, 0));
        var manifest1 = new PackageManifest { Path = "github.com/explicit/pkg", Version = new SemVersion(1, 0, 0), Variants = [new()] };
        var manifest2 = new PackageManifest { Path = "github.com/implicit/pkg", Version = new SemVersion(1, 0, 0), Variants = [new()] };

        await _service.AddInstalledPackage(explicitPkg, manifest1, [], true);
        await _service.AddInstalledPackage(implicitPkg, manifest2, [], false);

        var result = await _service.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit);

        Assert.Single(result);
        Assert.Equal(explicitPkg, result.First());
    }

    [Fact]
    public async Task GetInstalledPackages_ImplicitScope_ReturnsOnlyImplicit()
    {
        var explicitPkg = new PackageSpec(new PackageId("github.com/explicit/pkg", ""), new SemVersion(1, 0, 0));
        var implicitPkg = new PackageSpec(new PackageId("github.com/implicit/pkg", ""), new SemVersion(1, 0, 0));
        var manifest1 = new PackageManifest { Path = "github.com/explicit/pkg", Version = new SemVersion(1, 0, 0), Variants = [new()] };
        var manifest2 = new PackageManifest { Path = "github.com/implicit/pkg", Version = new SemVersion(1, 0, 0), Variants = [new()] };

        await _service.AddInstalledPackage(explicitPkg, manifest1, [], true);
        await _service.AddInstalledPackage(implicitPkg, manifest2, [], false);

        var result = await _service.GetInstalledPackages(IWorkspaceService.PackageScope.Implicit);

        Assert.Single(result);
        Assert.Equal(implicitPkg, result.First());
    }

    [Fact]
    public async Task RemoveInstalledPackage_RemovesFromState()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));
        var manifest = new PackageManifest { Path = "github.com/test/repo", Version = new SemVersion(1, 0, 0), Variants = [new()] };
        await _service.AddInstalledPackage(pkgSpec, manifest, [], true);

        await _service.RemoveInstalledPackage(pkgSpec);

        var installed = await _service.GetInstalledPackages(IWorkspaceService.PackageScope.All);
        Assert.Empty(installed);
    }

    [Fact]
    public async Task RemoveInstalledPackage_NotInstalled_ThrowsInvalidOperationException()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RemoveInstalledPackage(pkgSpec));
    }

    [Fact]
    public async Task UpdateInstalledPackageExplicitness_UpdatesExplicitness()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));
        var manifest = new PackageManifest { Path = "github.com/test/repo", Version = new SemVersion(1, 0, 0), Variants = [new()] };
        await _service.AddInstalledPackage(pkgSpec, manifest, [], true);

        await _service.UpdateInstalledPackageExplicitness(pkgSpec, false);

        var explicitPkgs = await _service.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit);
        var implicitPkgs = await _service.GetInstalledPackages(IWorkspaceService.PackageScope.Implicit);
        Assert.Empty(explicitPkgs);
        Assert.Single(implicitPkgs);
    }

    [Fact]
    public async Task UpdateInstalledPackageExplicitness_NotInstalled_ThrowsInvalidOperationException()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateInstalledPackageExplicitness(pkgSpec, false));
    }

    [Fact]
    public async Task CreateFileWithDirectory_HandlesRootFilePath()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));
        var manifest = new PackageManifest { Path = "github.com/test/repo", Version = new SemVersion(1, 0, 0), Variants = [new()] };

        await _service.AddInstalledPackage(pkgSpec, manifest, [], true);

        Assert.True(_fileSystem.File.Exists("tooth_lock.json"));
    }
}