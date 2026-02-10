using Flurl;
using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.Services;
using Lip.Core.SourceProviders;
using Moq;
using Semver;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.Services;

public class SourceServiceTests
{
    private readonly Mock<IGitRunner> _mockGitRunner;
    private readonly Mock<IUserInteraction> _mockUserInteraction;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly SourceService _service;

    public SourceServiceTests()
    {
        _mockGitRunner = new Mock<IGitRunner>();
        _mockUserInteraction = new Mock<IUserInteraction>();
        _mockCacheService = new Mock<ICacheService>();
        _service = new SourceService(
            _mockGitRunner.Object,
            _mockUserInteraction.Object,
            _mockCacheService.Object,
            githubProxy: null,
            goModuleProxy: new Url("https://proxy.golang.org"));
    }

    [Fact]
    public async Task Get_LocalPackageSpec_FileNotFound_ThrowsFileNotFoundException()
    {
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>(), @"C:\test");
        IFileInfo fileInfo = mockFileSystem.FileInfo.New(@"C:\nonexistent.zip");
        LocalPackageSpec localSpec = new(fileInfo, string.Empty);

        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.Get(localSpec));
    }

    [Fact]
    public async Task Get_LocalPackageSpec_FileExists_ReturnsArchiveSourceProvider()
    {
        MockFileSystem mockFileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\package.zip", new MockFileData("content") }
        }, @"C:\");
        IFileInfo fileInfo = mockFileSystem.FileInfo.New(@"C:\package.zip");
        LocalPackageSpec localSpec = new(fileInfo, string.Empty);

        ISourceProvider result = await _service.Get(localSpec);

        Assert.IsType<ArchiveSourceProvider>(result);
    }

    [Fact]
    public async Task Get_PackageSpec_AllSourcesFail_ThrowsAggregateException()
    {
        PackageSpec pkgSpec = new(new PackageId("example.com/pkg", string.Empty), new SemVersion(1, 0, 0));

        _mockCacheService.Setup(c => c.GetOrCreateDirectory(It.IsAny<string>(), It.IsAny<Func<IDirectoryInfo, Task>>()))
            .ThrowsAsync(new Exception("Cache failed"));

        _mockCacheService.Setup(c => c.GetOrCreateFile(It.IsAny<string>(), It.IsAny<Func<IFileInfo, Task>>()))
            .ThrowsAsync(new Exception("Cache file failed"));

        await Assert.ThrowsAsync<AggregateException>(() => _service.Get(pkgSpec));
    }

    [Fact]
    public async Task Get_PackageSpec_GitSucceeds_ReturnsDirectorySourceProvider()
    {
        PackageSpec pkgSpec = new(new PackageId("github.com/test/repo", string.Empty), new SemVersion(1, 0, 0));
        MockFileSystem mockFs = new();
        mockFs.AddDirectory(@"C:\cache\repo");
        IDirectoryInfo mockDir = mockFs.DirectoryInfo.New(@"C:\cache\repo");

        _mockCacheService.Setup(c => c.GetOrCreateFile(It.IsAny<string>(), It.IsAny<Func<IFileInfo, Task>>()))
            .ThrowsAsync(new Exception("Go Proxy failed"));

        _mockCacheService.Setup(c => c.GetOrCreateDirectory(It.IsAny<string>(), It.IsAny<Func<IDirectoryInfo, Task>>()))
            .ReturnsAsync(mockDir);

        ISourceProvider result = await _service.Get(pkgSpec);

        Assert.IsType<DirectorySourceProvider>(result);
    }

    [Fact]
    public async Task Get_PackageSpec_GitFails_GoModuleProxySucceeds_ReturnsGoModuleArchiveSourceProvider()
    {
        PackageSpec pkgSpec = new(new PackageId("example.com/test/pkg", string.Empty), new SemVersion(1, 0, 0));
        MockFileSystem mockFs = new();
        mockFs.AddFile(@"C:\cache\pkg.zip", new MockFileData("content"));
        IFileInfo mockFile = mockFs.FileInfo.New(@"C:\cache\pkg.zip");

        _mockCacheService.Setup(c => c.GetOrCreateDirectory(It.IsAny<string>(), It.IsAny<Func<IDirectoryInfo, Task>>()))
            .ThrowsAsync(new Exception("Git failed"));

        _mockCacheService.Setup(c => c.GetOrCreateFile(It.IsAny<string>(), It.IsAny<Func<IFileInfo, Task>>()))
            .ReturnsAsync(mockFile);

        ISourceProvider result = await _service.Get(pkgSpec);

        Assert.IsType<GoModuleArchiveSourceProvider>(result);
    }

    [Fact]
    public async Task Get_PackageSpec_WithGithubProxy_UsesProxy()
    {
        SourceService serviceWithProxy = new(
            _mockGitRunner.Object,
            _mockUserInteraction.Object,
            _mockCacheService.Object,
            githubProxy: new Url("https://ghproxy.com"),
            goModuleProxy: new Url("https://proxy.golang.org"));

        PackageSpec pkgSpec = new(new PackageId("github.com/test/repo", string.Empty), new SemVersion(1, 0, 0));
        MockFileSystem mockFs = new();
        mockFs.AddDirectory(@"C:\cache\repo");
        IDirectoryInfo mockDir = mockFs.DirectoryInfo.New(@"C:\cache\repo");

        _mockCacheService.Setup(c => c.GetOrCreateFile(It.IsAny<string>(), It.IsAny<Func<IFileInfo, Task>>()))
            .ThrowsAsync(new Exception("Go Proxy failed"));

        _mockCacheService.Setup(c => c.GetOrCreateDirectory(It.Is<string>(k => k.Contains("ghproxy.com")), It.IsAny<Func<IDirectoryInfo, Task>>()))
            .ReturnsAsync(mockDir);

        ISourceProvider result = await serviceWithProxy.Get(pkgSpec);

        Assert.IsType<DirectorySourceProvider>(result);
    }

    [Fact]
    public async Task Get_PackageSpec_MajorVersion2_AddsIncompatibleMetadata()
    {
        PackageSpec pkgSpec = new(new PackageId("example.com/test/pkg", string.Empty), new SemVersion(2, 0, 0));
        MockFileSystem mockFs = new();
        mockFs.AddFile(@"C:\cache\pkg.zip", new MockFileData("content"));
        IFileInfo mockFile = mockFs.FileInfo.New(@"C:\cache\pkg.zip");

        _mockCacheService.Setup(c => c.GetOrCreateDirectory(It.IsAny<string>(), It.IsAny<Func<IDirectoryInfo, Task>>()))
            .ThrowsAsync(new Exception("Git failed"));

        _mockCacheService.Setup(c => c.GetOrCreateFile(It.IsAny<string>(), It.IsAny<Func<IFileInfo, Task>>()))
            .ReturnsAsync(mockFile);

        ISourceProvider result = await _service.Get(pkgSpec);

        Assert.IsType<GoModuleArchiveSourceProvider>(result);
    }

    [Fact]
    public async Task Get_RemotePackageSpec_ReturnsArchiveSourceProvider()
    {
        MockFileSystem mockFs = new();
        mockFs.AddFile(@"C:\cache\remote.zip", new MockFileData("content"));
        IFileInfo mockFile = mockFs.FileInfo.New(@"C:\cache\remote.zip");

        _mockCacheService.Setup(c => c.GetOrCreateFile(It.IsAny<string>(), It.IsAny<Func<IFileInfo, Task>>()))
            .ReturnsAsync(mockFile);

        RemotePackageSpec remoteSpec = new(new Url("https://example.com/package.zip"), string.Empty);

        ISourceProvider result = await _service.Get(remoteSpec);

        Assert.IsType<ArchiveSourceProvider>(result);
    }

    [Fact]
    public async Task Get_Url_IsArchiveTrue_ReturnsArchiveSourceProvider()
    {
        MockFileSystem mockFs = new();
        mockFs.AddFile(@"C:\cache\file.zip", new MockFileData("content"));
        IFileInfo mockFile = mockFs.FileInfo.New(@"C:\cache\file.zip");

        _mockCacheService.Setup(c => c.GetOrCreateFile(It.IsAny<string>(), It.IsAny<Func<IFileInfo, Task>>()))
            .ReturnsAsync(mockFile);

        ISourceProvider result = await _service.Get(new Url("https://example.com/file.zip"), isArchive: true);

        Assert.IsType<ArchiveSourceProvider>(result);
    }

    [Fact]
    public async Task Get_Url_IsArchiveFalse_ReturnsSingleFileSourceProvider()
    {
        MockFileSystem mockFs = new();
        mockFs.AddFile(@"C:\cache\file.txt", new MockFileData("content"));
        IFileInfo mockFile = mockFs.FileInfo.New(@"C:\cache\file.txt");

        _mockCacheService.Setup(c => c.GetOrCreateFile(It.IsAny<string>(), It.IsAny<Func<IFileInfo, Task>>()))
            .ReturnsAsync(mockFile);

        ISourceProvider result = await _service.Get(new Url("https://example.com/file.txt"), isArchive: false);

        Assert.IsType<SingleFileSourceProvider>(result);
    }
}