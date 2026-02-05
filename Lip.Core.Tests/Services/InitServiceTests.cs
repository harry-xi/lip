using Moq;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests;

using Lip.Core.Context;
using Lip.Core.Services;

public class InitServiceTests
{
    private static readonly string s_workspacePath = OperatingSystem.IsWindows() ? Path.Join("C:", "path", "to", "workspace") : Path.Join("/", "path", "to", "workspace");

    [Fact]
    public async Task Init_Passes()
    {
        // Arrange.
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { s_workspacePath, new MockDirectoryData() },
        }, currentDirectory: s_workspacePath);

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_workspacePath, s_workspacePath);
        var cacheManager = new Mock<ICacheManager>();
        var workspaceManager = new WorkspaceManager(
            context.Object.FileSystem,
            context.Object.CommandRunner,
            context.Object.Logger,
            context.Object.UserInteraction,
            cacheManager.Object,
            pathManager);
        var initService = new InitService(context.Object, pathManager);


        // Act.
        await initService.Init();

        // Assert.
        Assert.True(fileSystem.File.Exists(Path.Join(s_workspacePath, "tooth.json")));
        Assert.Equal($$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/org/package",
                "version": "0.1.0",
                "info": {
                    "name": "",
                    "description": "",
                    "tags": []
                },
                "variants": []
            }
            """.ReplaceLineEndings(),
            fileSystem.File.ReadAllText(Path.Join(s_workspacePath, "tooth.json")).ReplaceLineEndings());
    }





    [Fact]
    public async Task Init_ManifestFileExists_ThrowsInvalidOperationException()
    {
        // Arrange.
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { s_workspacePath, new MockDirectoryData() },
            { Path.Join(s_workspacePath, "tooth.json"), new MockFileData("content") },
        }, currentDirectory: s_workspacePath);

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_workspacePath, s_workspacePath);
        var cacheManager = new Mock<ICacheManager>();
        var workspaceManager = new WorkspaceManager(
            context.Object.FileSystem,
            context.Object.CommandRunner,
            context.Object.Logger,
            context.Object.UserInteraction,
            cacheManager.Object,
            pathManager);
        var initService = new InitService(context.Object, pathManager);


        // Act and assert.
        await Assert.ThrowsAsync<InvalidOperationException>(() => initService.Init());
    }


}