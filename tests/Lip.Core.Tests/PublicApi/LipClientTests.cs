using Flurl;
using Lip.Core.Entities;
using Lip.Core.PackageRegistries;
using Lip.Core.PublicApi;
using Lip.Core.Services;
using Moq;
using Semver;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Xunit;

namespace Lip.Core.Tests.PublicApi;

public class LipClientTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly Mock<ICacheService> _cacheService;
    private readonly Mock<IConfigService> _configService;
    private readonly Mock<IInstallService> _installService;
    private readonly Mock<IPackageRegistry> _packageRegistry;
    private readonly Mock<IWorkspaceService> _workspaceService;
    private readonly LipClient _client;

    public LipClientTests()
    {
        _fileSystem = new MockFileSystem();
        _cacheService = new Mock<ICacheService>();
        _configService = new Mock<IConfigService>();
        _installService = new Mock<IInstallService>();
        _packageRegistry = new Mock<IPackageRegistry>();
        _workspaceService = new Mock<IWorkspaceService>();

        _client = new LipClient(
            _fileSystem,
            _cacheService.Object,
            _configService.Object,
            _installService.Object,
            _packageRegistry.Object,
            _workspaceService.Object);
    }

    [Fact]
    public async Task CacheClean_CallsService()
    {
        await _client.CacheClean();
        _cacheService.Verify(s => s.Clean(), Times.Once);
    }

    [Fact]
    public async Task ConfigGet_ReturnsValue()
    {
        var config = new RuntimeConfig { GithubProxy = new Url("https://proxy.com") };
        _configService.Setup(s => s.LoadConfig()).ReturnsAsync(config);

        var result = await _client.ConfigGet("github_proxy");

        Assert.Equal("https://proxy.com", result);
    }

    [Fact]
    public async Task ConfigSet_SavesConfig()
    {
        var config = new RuntimeConfig();
        _configService.Setup(s => s.LoadConfig()).ReturnsAsync(config);

        await _client.ConfigSet("github_proxy", "https://new_proxy.com");

        _configService.Verify(s => s.SaveConfig(It.Is<RuntimeConfig>(c => c.GithubProxy != null && c.GithubProxy.ToString() == "https://new_proxy.com")), Times.Once);
    }

    [Fact]
    public async Task ConfigDelete_RemovesKey()
    {
        var config = new RuntimeConfig { GithubProxy = new Url("https://proxy.com") };
        _configService.Setup(s => s.LoadConfig()).ReturnsAsync(config);

        await _client.ConfigDelete("github_proxy");

        _configService.Verify(s => s.SaveConfig(It.Is<RuntimeConfig>(c => c.GithubProxy == null)), Times.Once);
    }

    [Fact]
    public async Task ConfigList_ReturnsAllKeys()
    {
        var config = new RuntimeConfig { GithubProxy = new Url("https://proxy.com") };
        _configService.Setup(s => s.LoadConfig()).ReturnsAsync(config);

        var result = await _client.ConfigList();

        Assert.True(result.ContainsKey("github_proxy"));
        Assert.Equal("https://proxy.com", result["github_proxy"]);
    }

    [Fact]
    public async Task Init_CreatesToothJson()
    {
        await _client.Init();

        Assert.True(_fileSystem.File.Exists("tooth.json"));
        var content = _fileSystem.File.ReadAllText("tooth.json");
        Assert.Contains("github.com/user/repo", content);
    }

    [Fact]
    public async Task Install_ParsesAndCallsService()
    {
        var packages = new[] { "github.com/test/repo@1.0.0" };
        await _client.Install(packages, false, false, false);

        _installService.Verify(s => s.InstallPackages(
            It.Is<IEnumerable<PackageSpec>>(p => p.Count() == 1 && p.First().Id.ToString() == "github.com/test/repo"),
            It.Is<IEnumerable<PackageId>>(p => !p.Any()),
            It.Is<IEnumerable<LocalPackageSpec>>(p => !p.Any()),
            It.Is<IEnumerable<RemotePackageSpec>>(p => !p.Any()),
            false,
            false,
            false), Times.Once);
    }

    [Fact]
    public async Task Install_ParsesFlexiblePackage()
    {
        var packages = new[] { "github.com/test/repo" };
        await _client.Install(packages, false, false, false);

        _installService.Verify(s => s.InstallPackages(
            It.Is<IEnumerable<PackageSpec>>(p => !p.Any()),
            It.Is<IEnumerable<PackageId>>(p => p.Count() == 1 && p.First().ToString() == "github.com/test/repo"),
            It.Is<IEnumerable<LocalPackageSpec>>(p => !p.Any()),
            It.Is<IEnumerable<RemotePackageSpec>>(p => !p.Any()),
            false,
            false,
            false), Times.Once);
    }

    [Fact]
    public async Task List_ReturnsPackages()
    {
        var explicitPkg = new PackageSpec(new PackageId("github.com/test/pkg1", ""), new SemVersion(1));
        var implicitPkg = new PackageSpec(new PackageId("github.com/test/pkg2", ""), new SemVersion(1));

        _workspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.Explicit))
            .ReturnsAsync([explicitPkg]);
        _workspaceService.Setup(w => w.GetInstalledPackages(IWorkspaceService.PackageScope.Implicit))
            .ReturnsAsync([implicitPkg]);

        var (explicitInstalled, implicitInstalled) = await _client.List();

        Assert.Single(explicitInstalled);
        Assert.Single(implicitInstalled);
        Assert.Equal("github.com/test/pkg1", explicitInstalled.First().Id.ToString());
    }

    [Fact]
    public async Task Uninstall_ParsesAndCallsService()
    {
        var packages = new[] { "github.com/test/repo" };
        await _client.Uninstall(packages, false, false, false);

        _installService.Verify(s => s.UninstallPackages(
            It.Is<IEnumerable<PackageId>>(p => p.Single().ToString() == "github.com/test/repo"),
            false,
            false,
            false), Times.Once);
    }

    [Fact]
    public async Task Update_ParsesAndCallsService()
    {
        var packages = new[] { "github.com/test/repo@2.0.0" };
        await _client.Update(packages, false, false);

        _installService.Verify(s => s.UpdatePackages(
            It.Is<IEnumerable<PackageSpec>>(p => p.Single().Version.Major == 2),
            It.IsAny<IEnumerable<PackageId>>(),
            It.IsAny<IEnumerable<LocalPackageSpec>>(),
            It.IsAny<IEnumerable<RemotePackageSpec>>(),
            false,
            false), Times.Once);
    }

    [Fact]
    public async Task Update_ParsesFlexiblePackage()
    {
        var packages = new[] { "github.com/test/repo" };
        await _client.Update(packages, false, false);

        _installService.Verify(s => s.UpdatePackages(
            It.Is<IEnumerable<PackageSpec>>(p => !p.Any()),
            It.Is<IEnumerable<PackageId>>(p => p.Count() == 1 && p.First().ToString() == "github.com/test/repo"),
            It.IsAny<IEnumerable<LocalPackageSpec>>(),
            It.IsAny<IEnumerable<RemotePackageSpec>>(),
            false,
            false), Times.Once);
    }

    [Fact]
    public async Task View_ReturnsManifestJson()
    {
        var pkgSpec = new PackageSpec(new PackageId("github.com/test/repo", ""), new SemVersion(1, 0, 0));
        var manifest = new PackageManifest { Path = "github.com/test/repo", Version = new SemVersion(1, 0, 0) };
        _packageRegistry.Setup(r => r.GetPackageManifest(It.IsAny<PackageSpec>())).ReturnsAsync(manifest);

        var result = await _client.View("github.com/test/repo@1.0.0");

        Assert.Contains("github.com/test/repo", result);
    }
    [Fact]
    public async Task Create_ReturnsInstance()
    {
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger>();
        var fileSystem = new MockFileSystem();

        // Setup config structure if needed by ConfigService internals, 
        // though default usually works if files missing (it creates them or returns default).
        // If it fails we might need to pre-create directories.

        var client = await LipClient.Create(logger.Object, fileSystem);

        Assert.NotNull(client);
    }

    [Fact]
    public async Task Install_ParsesLocalPackage()
    {
        var localPath = @"c:\path\to\package.zip";
        _fileSystem.AddFile(localPath, new MockFileData("content"));
        var packages = new[] { localPath };

        await _client.Install(packages, false, false, false);

        _installService.Verify(s => s.InstallPackages(
            It.Is<IEnumerable<PackageSpec>>(p => !p.Any()),
            It.Is<IEnumerable<PackageId>>(p => !p.Any()),
            It.Is<IEnumerable<LocalPackageSpec>>(p => p.Count() == 1 && p.First().ArchiveFile.FullName == "c:\\path\\to\\package.zip"),
            It.Is<IEnumerable<RemotePackageSpec>>(p => !p.Any()),
            false,
            false,
            false), Times.Once);
    }

    [Fact]
    public async Task Install_ParsesRemotePackage()
    {
        var remoteUrl = "https://example.com/package.zip";
        var packages = new[] { remoteUrl };

        await _client.Install(packages, false, false, false);

        _installService.Verify(s => s.InstallPackages(
            It.Is<IEnumerable<PackageSpec>>(p => !p.Any()),
            It.Is<IEnumerable<PackageId>>(p => !p.Any()),
            It.Is<IEnumerable<LocalPackageSpec>>(p => !p.Any()),
            It.Is<IEnumerable<RemotePackageSpec>>(p => p.Count() == 1 && p.First().ArchiveUrl.ToString() == remoteUrl),
            false,
            false,
            false), Times.Once);
    }

    [Fact]
    public async Task Install_ThrowsAggregateException_WhenAllParsesFail()
    {
        // Assuming PackageSpec.Parse, PackageId.Parse, etc. throw on this.
        // Actually PackageId might be very permissive. We need a string that definitely fails everything.
        // If PackageId accepts anything, then this test is hard. 
        // But usually PackageId follows some format (e.g. name or name@version). 
        // If PackageId.Parse fails, then we get exception.

        // Let's assume an empty string or invalid chars might fail.
        // But to be safe, I'll rely on the fact that if it fails, it throws.
        // If PackageId parses everything, then "Install_ParsesFlexiblePackage" covers it.
        // Let's assume " " (whitespace) might fail parse?

        // If I cannot find a string that fails, I cannot test the exception path easily without mocking the static Parse methods (which I can't).
        // However, looking at the code:
        // PackageId.Parse(package) is tried after PackageSpec.Parse.
        // If PackageId.Parse fails, it tries LocalPackageSpec.

        // Let's try to verify behavior with a likely invalid string for ALL parsers.
        // Attempting with empty string might be a good candidate if parsers validate input.
        var packages = new[] { "" };

        await Assert.ThrowsAsync<AggregateException>(() => _client.Install(packages, false, false, false));
    }

    [Fact]
    public async Task Update_ParsesLocalAndRemotePackages()
    {
        var localPath = @"c:\path\to\package.zip";
        _fileSystem.AddFile(localPath, new MockFileData("content"));
        var remoteUrl = "https://example.com/package.zip";
        var packages = new[] { localPath, remoteUrl };

        await _client.Update(packages, false, false);

        _installService.Verify(s => s.UpdatePackages(
            It.IsAny<IEnumerable<PackageSpec>>(),
            It.IsAny<IEnumerable<PackageId>>(),
            It.Is<IEnumerable<LocalPackageSpec>>(p => p.Count() == 1 && p.First().ArchiveFile.FullName == @"c:\path\to\package.zip"),
            It.Is<IEnumerable<RemotePackageSpec>>(p => p.Count() == 1 && p.First().ArchiveUrl.ToString() == remoteUrl),
            false,
            false), Times.Once);
    }

    [Fact]
    public async Task Update_ThrowsAggregateException_WhenParseFails()
    {
        var packages = new[] { "" };
        await Assert.ThrowsAsync<AggregateException>(() => _client.Update(packages, false, false));
    }

    [Fact]
    public async Task Migrate_TransformsManifest()
    {
        var inputFile = @"c:\input.json";
        var outputFile = @"c:\output.json";

        var inputJson = @"{
            ""format_version"": 2,
            ""tooth"": ""github.com/test/repo"",
            ""version"": ""1.0.0"",
            ""info"": { ""name"": ""Test"", ""description"": ""Test package"", ""author"": ""Test Author"", ""tags"": [] }
        }";
        _fileSystem.AddFile(inputFile, new MockFileData(inputJson));

        await _client.Migrate(inputFile, outputFile);

        Assert.True(_fileSystem.File.Exists(outputFile));
        var content = _fileSystem.File.ReadAllText(outputFile);
        Assert.Contains("github.com/test/repo", content);
    }
}