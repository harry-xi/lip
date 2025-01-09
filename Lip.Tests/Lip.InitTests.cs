using System.IO.Abstractions.TestingHelpers;
using Moq;

namespace Lip.Tests;

public partial class LipTests
{
    public static Mock<Serilog.ILogger> CreateMockLogger()
    {
        var logger = new Mock<Serilog.ILogger>();
        logger.Setup(l => l.Warning(It.IsAny<string>()));
        logger.Setup(l => l.Information(It.IsAny<string>()));
        return logger;
    }

    [Fact]
    public async Task Init_NoInteraction_Passes()
    {
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\path\to\workspace", new MockDirectoryData() },
        }, currentDirectory: @"C:\path\to\workspace");
        Mock<Serilog.ILogger> logger = CreateMockLogger();
        Lip lip = new(new(), fileSystem, logger.Object);
        Lip.InitArgs args = new()
        {
            InitAuthor = "Example Author",
            InitAvatarUrl = "https://example.com/avatar.png",
            InitDescription = "An example package.",
            InitName = "Example Package",
            InitTooth = "example.com/org/package",
            InitVersion = "0.1.0",
            Yes = true,
        };

        await lip.Init(args);

        logger.Verify(l => l.Information(It.IsAny<string>()), Times.Exactly(1));
        Assert.True(fileSystem.File.Exists(@"C:\path\to\workspace\tooth.json"));
        Assert.Equal("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/org/package",
                "version": "0.1.0",
                "info": {
                    "name": "Example Package",
                    "description": "An example package.",
                    "author": "Example Author",
                    "avatar_url": "https://example.com/avatar.png"
                }
            }
            """, fileSystem.File.ReadAllText(@"C:\path\to\workspace\tooth.json"));
    }

    [Fact]
    public async Task Init_Interactive_Passes()
    {
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\path\to\workspace", new MockDirectoryData() },
        }, currentDirectory: @"C:\path\to\workspace");
        Mock<Serilog.ILogger> logger = CreateMockLogger();
        Lip lip = new(new(), fileSystem, logger.Object);
        Lip.InitArgs args = new();
        lip.OnUserInteractionPrompted += (sender, e) =>
        {
            if (e.EventType == Lip.UserInteractionEventType.InitTooth)
            {
                e.Input = "example.com/org/package";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitVersion)
            {
                e.Input = "0.1.0";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitName)
            {
                e.Input = "Example Package";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitDescription)
            {
                e.Input = "An example package.";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitAuthor)
            {
                e.Input = "Example Author";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitAvatarUrl)
            {
                e.Input = "https://example.com/avatar.png";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitConfirm)
            {
                e.Input = "y";
            }
        };

        await lip.Init(args);

        logger.Verify(l => l.Information(It.IsAny<string>()), Times.Exactly(1));
        Assert.True(fileSystem.File.Exists(@"C:\path\to\workspace\tooth.json"));
        Assert.Equal("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/org/package",
                "version": "0.1.0",
                "info": {
                    "name": "Example Package",
                    "description": "An example package.",
                    "author": "Example Author",
                    "avatar_url": "https://example.com/avatar.png"
                }
            }
            """, fileSystem.File.ReadAllText(@"C:\path\to\workspace\tooth.json"));
    }

    [Fact]
    public async Task Init_IteractiveCancelled_ThrowsOperationCanceledException()
    {
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\path\to\workspace", new MockDirectoryData() },
        }, currentDirectory: @"C:\path\to\workspace");
        Mock<Serilog.ILogger> logger = CreateMockLogger();
        Lip lip = new(new(), fileSystem, logger.Object);
        Lip.InitArgs args = new();
        lip.OnUserInteractionPrompted += (sender, e) =>
        {
            if (e.EventType == Lip.UserInteractionEventType.InitTooth)
            {
                e.Input = "example.com/org/package";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitVersion)
            {
                e.Input = "0.1.0";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitName)
            {
                e.Input = "Example Package";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitDescription)
            {
                e.Input = "An example package.";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitAuthor)
            {
                e.Input = "Example Author";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitAvatarUrl)
            {
                e.Input = "https://example.com/avatar.png";
            }
            else if (e.EventType == Lip.UserInteractionEventType.InitConfirm)
            {
                e.Input = "n";
            }
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() => lip.Init(args));
    }

    [Fact]
    public async Task Init_POSIX_Passes()
    {
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { "/path/to/workspace", new MockDirectoryData() },
        }, currentDirectory: "/path/to/workspace");
        Mock<Serilog.ILogger> logger = CreateMockLogger();
        Lip lip = new(new(), fileSystem, logger.Object);
        Lip.InitArgs args = new()
        {
            InitAuthor = "Example Author",
            InitAvatarUrl = "https://example.com/avatar.png",
            InitDescription = "An example package.",
            InitName = "Example Package",
            InitTooth = "example.com/org/package",
            InitVersion = "0.1.0",
            Yes = true,
        };

        await lip.Init(args);

        logger.Verify(l => l.Information(It.IsAny<string>()), Times.Exactly(1));
        Assert.True(fileSystem.File.Exists("/path/to/workspace/tooth.json"));
        Assert.Equal("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/org/package",
                "version": "0.1.0",
                "info": {
                    "name": "Example Package",
                    "description": "An example package.",
                    "author": "Example Author",
                    "avatar_url": "https://example.com/avatar.png"
                }
            }
            """, fileSystem.File.ReadAllText("/path/to/workspace/tooth.json"));
    }

    [Fact]
    public async Task Init_WorkspaceDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>(), currentDirectory: @"C:\path\to\workspace");
        Mock<Serilog.ILogger> logger = CreateMockLogger();
        Lip lip = new(new(), fileSystem, logger.Object);
        Lip.InitArgs args = new()
        {
            Workspace = @"invalid_workspace",
            Yes = true,
        };

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => lip.Init(args));
    }

    [Fact]
    public async Task Init_FileExists_ThrowsInvalidOperationException()
    {
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\path\to\workspace", new MockDirectoryData() },
            { @"C:\path\to\workspace\tooth.json", new MockFileData("content") },
        }, currentDirectory: @"C:\path\to\workspace");
        Mock<Serilog.ILogger> logger = CreateMockLogger();
        Lip lip = new(new(), fileSystem, logger.Object);
        Lip.InitArgs args = new()
        {
            Workspace = @"C:\path\to\workspace",
            Yes = true,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => lip.Init(args));
    }

    [Fact]
    public async Task Init_ForceFileExists_Passes()
    {
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            { @"C:\path\to\workspace", new MockDirectoryData() },
            { @"C:\path\to\workspace\tooth.json", new MockFileData("content") },
        }, currentDirectory: @"C:\path\to\workspace");
        Mock<Serilog.ILogger> logger = CreateMockLogger();
        Lip lip = new(new(), fileSystem, logger.Object);
        Lip.InitArgs args = new()
        {
            Force = true,
            Workspace = @"C:\path\to\workspace",
            Yes = true,
        };

        await lip.Init(args);

        logger.Verify(l => l.Information(It.IsAny<string>()), Times.Exactly(1));
        logger.Verify(l => l.Warning(It.IsAny<string>()), Times.Exactly(1));
        Assert.True(fileSystem.File.Exists(@"C:\path\to\workspace\tooth.json"));
    }
}
