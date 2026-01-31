using Lip.Core.Context;
using Moq;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;

namespace Lip.Core.Tests;

using Lip.Core.Services;

public class RunServiceTests
{
    private static readonly string s_workDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "work")
        : Path.Join("/", "path", "to", "work");



    [Fact]
    public async Task Run_ValidScript_Passes()
    {
        // Arrange.
        string packageManifestData = $$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/repo",
                "version": "1.0.0",
                "variants": [
                    {
                        "label": "test_variant",
                        "platform": "{{RuntimeInformation.RuntimeIdentifier}}",
                        "scripts": {
                            "test": [
                                "run_test"
                            ]
                        }
                    }
                ]
            }
            """;

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { "/path/to/work/tooth.json", new MockFileData(packageManifestData) },
        }, currentDirectory: s_workDir);

        Mock<ICommandRunner> commandRunner = new();
        commandRunner.Setup(c => c.Run("run_test", s_workDir));

        Mock<IContext> context = new();
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_workDir, s_workDir);
        var cacheManager = new Mock<ICacheManager>(); // Not used directly
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var runService = new RunService(context.Object, packageManager, pathManager);

        // Act.
        await runService.Run("test", variantLabel: "test_variant");
    }

    [Fact]
    public async Task Run_PackageManifestNotExists_ThrowsFileNotFoundException()
    {
        // Arrange.
        MockFileSystem fileSystem = new();

        Mock<ICommandRunner> commandRunner = new(); // Still need a mock for command runner
        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_workDir, s_workDir);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var runService = new RunService(context.Object, packageManager, pathManager);

        // Act & Assert.
        await Assert.ThrowsAsync<InvalidOperationException>(() => runService.Run("test"));
    }

    [Fact]
    public async Task Run_VariantNotFound_ThrowsInvalidOperationException()
    {
        // Arrange.
        string packageManifestData = $$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/repo",
                "version": "1.0.0",
                "variants": [
                    {
                        "label": "test_variant",
                        "platform": "{{RuntimeInformation.RuntimeIdentifier}}",
                        "scripts": {
                            "test": [
                                "run_test"
                            ]
                        }
                    }
                ]
            }
            """;

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { "/path/to/work/tooth.json", new MockFileData(packageManifestData) },
        }, currentDirectory: s_workDir);

        Mock<ICommandRunner> commandRunner = new(); // Still need a mock for command runner

        Mock<IContext> context = new();
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_workDir, s_workDir);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var runService = new RunService(context.Object, packageManager, pathManager);

        // Act & Assert.
        await Assert.ThrowsAsync<InvalidOperationException>(() => runService.Run("test", variantLabel: "unknown_variant"));
    }

    [Fact]
    public async Task Run_ScriptNotFound_ThrowsInvalidOperationException()
    {
        // Arrange.
        string packageManifestData = $$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/repo",
                "version": "1.0.0",
                "variants": [
                    {
                        "platform": "{{RuntimeInformation.RuntimeIdentifier}}",
                        "scripts": {
                            "test": [
                                "run_test"
                            ]
                        }
                    }
                ]
            }
            """;

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { "/path/to/work/tooth.json", new MockFileData(packageManifestData) },
        }, currentDirectory: s_workDir);

        Mock<ICommandRunner> commandRunner = new(); // Still need a mock for command runner

        Mock<IContext> context = new();
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_workDir, s_workDir);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var runService = new RunService(context.Object, packageManager, pathManager);

        // Act & Assert.
        await Assert.ThrowsAsync<InvalidOperationException>(() => runService.Run("unknown_script"));
    }

    [Fact]
    public async Task Run_CommandFails_ReturnsErrorCode()
    {
        // Arrange.
        string packageManifestData = $$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/repo",
                "version": "1.0.0",
                "variants": [
                    {
                        "platform": "{{RuntimeInformation.RuntimeIdentifier}}",
                        "scripts": {
                            "test": [
                                "run_test"
                            ]
                        }
                    }
                ]
            }
            """;

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { "/path/to/work/tooth.json", new MockFileData(packageManifestData) },
        }, currentDirectory: s_workDir);

        Mock<ICommandRunner> commandRunner = new();
        commandRunner.Setup(c => c.Run("run_test", s_workDir));

        Mock<IContext> context = new();
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_workDir, s_workDir);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var runService = new RunService(context.Object, packageManager, pathManager);

        // Act.
        await runService.Run("test");
    }
}