using Lip.Core.Context;
using Lip.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Semver;
using SharpCompress.Readers;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using System.Text;

namespace Lip.Core.Tests;

public class PackServiceTests
{
    private static readonly string s_workspacePath = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "workspace")
        : Path.Join("/", "path", "to", "workspace");

    private static PackageManifest.ScriptsType CreateEmptyScripts() => new()
    {
        PreInstall = [],
        Install = [],
        PostInstall = [],
        PrePack = [],
        PostPack = [],
        PreUninstall = [],
        Uninstall = [],
        PostUninstall = [],

    };

    private static PackageManifest.Variant CreateDefaultVariant() => new()
    {
        Label = "",
        Platform = RuntimeInformation.RuntimeIdentifier,
        Dependencies = [],
        Assets = [],
        PreserveFiles = [],
        RemoveFiles = [],
        Scripts = CreateEmptyScripts()
    };



    [Fact]
    public async Task Pack_NoPackageManifest_ThrowsInvalidOperationException()
    {
        // Arrange.
        MockFileSystem fileSystem = new();

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_workspacePath, s_workspacePath);
        var cacheManager = new Mock<ICacheManager>(); // Not used directly in this test but required for PackageManager
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var packService = new PackService(context.Object, packageManager, pathManager);

        // Act & assert.
        await Assert.ThrowsAsync<InvalidOperationException>(() => packService.Pack("output"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Pack_DefaultArguments_CreatesArchive(int variantListIndex)
    {
        // Arrange.
        List<PackageManifest.Variant> variants = [];

        if (variantListIndex == 1)
        {
            variants.Add(CreateDefaultVariant());
        }
        else if (variantListIndex == 2)
        {
            var v = CreateDefaultVariant() with
            {
                Assets = [
                new() {
                    Type = PackageManifest.Asset.TypeEnum.Self,
                    Urls = [],
                    Placements = []
                },
                new() {
                    Type = PackageManifest.Asset.TypeEnum.Self,
                    Urls = [],
                    Placements = [
                        new() {
                            Type = PackageManifest.Placement.TypeEnum.File,
                            Src = "file1",
                            Dest = "file1"
                        },
                        new() {
                            Type = PackageManifest.Placement.TypeEnum.Dir,
                            Src = "dir",
                            Dest = "dir"
                        }
                    ]
                }
            ]
            };
            variants.Add(v);
        }
        else if (variantListIndex == 3)
        {
            variants.Add(CreateDefaultVariant());
        }
        else if (variantListIndex == 4)
        {
            var scripts = CreateEmptyScripts() with
            {
                PrePack = ["echo 'pre-pack script'"],
                PostPack = ["echo 'post-pack script'"]
            };
            var v = CreateDefaultVariant() with { Scripts = scripts };
            variants.Add(v);
        }

        PackageManifest packageManifest = new()
        {
            FormatVersion = PackageManifest.DefaultFormatVersion,
            FormatUuid = PackageManifest.DefaultFormatUuid,
            ToothPath = "example.com/pkg",
            Version = SemVersion.Parse("1.0.0"),
            Info = new PackageManifest.InfoType
            {
                Name = "",
                Description = "",
                Tags = [],
                AvatarUrl = ""
            },
            Variants = variants
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
        {
                Path.Join(s_workspacePath, "tooth.json"),
                new MockFileData(packageManifest.ToJsonBytes())
        },
        {
                Path.Join(s_workspacePath, "file1"),
                new MockFileData("file1")
        },
        {
                Path.Join(s_workspacePath, "dir", "file2"),
                new MockFileData("file2")
        },
        {
                Path.Join(s_workspacePath, "dir", "dir", "file3"),
                new MockFileData("file3")
        }
        }, s_workspacePath);

        Mock<ICommandRunner> commandRunner = new();
        commandRunner.Setup(c => c.Run(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        Mock<ILogger> logger = new();

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var pathManager = new PathManager(fileSystem, s_workspacePath, s_workspacePath);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var packService = new PackService(context.Object, packageManager, pathManager);

        // Act.
        await packService.Pack("output");

        // Assert.
        Assert.True(fileSystem.File.Exists("output"));
        Assert.Equal(
            Encoding.UTF8.GetString(packageManifest.ToJsonBytes()),
            ReadArchiveEntryContent(fileSystem, "output", "tooth.json"));

        if (variantListIndex == 2)
        {
            Assert.Equal("file1", ReadArchiveEntryContent(fileSystem, "output", "file1"));
            Assert.Equal("file2", ReadArchiveEntryContent(fileSystem, "output", "dir/file2"));
            Assert.Equal("file3", ReadArchiveEntryContent(fileSystem, "output", "dir/dir/file3"));
        }

        if (variantListIndex == 4)
        {
            commandRunner.Verify(c => c.Run("echo 'pre-pack script'", s_workspacePath), Times.Once);
            commandRunner.Verify(c => c.Run("echo 'post-pack script'", s_workspacePath), Times.Once);
        }
    }

    [Fact]
    public async Task Pack_GlobFileMatches_CreatesArchive()
    {
        // Arrange.
        var variant = CreateDefaultVariant() with
        {
            Assets = [
            new() {
                Type = PackageManifest.Asset.TypeEnum.Self,
                Urls = [],
                Placements = [
                    new() {
                        Type = PackageManifest.Placement.TypeEnum.File,
                        Src = "*.txt",
                        Dest = "files"
                    }
                ]
            }
        ]
        };

        PackageManifest packageManifest = new()
        {
            FormatVersion = PackageManifest.DefaultFormatVersion,
            FormatUuid = PackageManifest.DefaultFormatUuid,
            ToothPath = "example.com/pkg",
            Version = SemVersion.Parse("1.0.0"),
            Info = new PackageManifest.InfoType
            {
                Name = "",
                Description = "",
                Tags = [],
                AvatarUrl = ""
            },
            Variants = [variant]
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
        {
                Path.Join(s_workspacePath, "tooth.json"),
                new MockFileData(packageManifest.ToJsonBytes())
        },
        {
                Path.Join(s_workspacePath, "file1.txt"),
                new MockFileData("file1")
        },
        {
                Path.Join(s_workspacePath, "file2.txt"),
                new MockFileData("file2")
        },
        {
                Path.Join(s_workspacePath, "file3.md"),
                new MockFileData("file3")
        }
        }, s_workspacePath);

        Mock<ICommandRunner> commandRunner = new();
        commandRunner.Setup(c => c.Run(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        Mock<ILogger> logger = new();

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var pathManager = new PathManager(fileSystem, s_workspacePath, s_workspacePath);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var packService = new PackService(context.Object, packageManager, pathManager);

        // Act.
        await packService.Pack("output");

        // Assert.
        Assert.True(fileSystem.File.Exists("output"));
        Assert.Equal(
            Encoding.UTF8.GetString(packageManifest.ToJsonBytes()),
            ReadArchiveEntryContent(fileSystem, "output", "tooth.json"));
        Assert.Equal("file1", ReadArchiveEntryContent(fileSystem, "output", "file1.txt"));
        Assert.Equal("file2", ReadArchiveEntryContent(fileSystem, "output", "file2.txt"));
        Assert.Null(ReadArchiveEntryContent(fileSystem, "output", "file3.md"));
    }

    [Fact]
    public async Task Pack_InvalidPlaceType_ThrowsNotImplementedException()
    {
        // Arrange.
        var variant = CreateDefaultVariant() with
        {
            Assets = [
            new() {
                Type = PackageManifest.Asset.TypeEnum.Self,
                Urls = [],
                Placements = [
                    new() {
                        Type = (PackageManifest.Placement.TypeEnum)int.MaxValue,
                        Src = "file1",
                        Dest = "file1"
                    }
                ]
            }
        ]
        };

        PackageManifest packageManifest = new()
        {
            FormatVersion = PackageManifest.DefaultFormatVersion,
            FormatUuid = PackageManifest.DefaultFormatUuid,
            ToothPath = "example.com/pkg",
            Version = SemVersion.Parse("1.0.0"),
            Info = new PackageManifest.InfoType
            {
                Name = "",
                Description = "",
                Tags = [],
                AvatarUrl = ""
            },
            Variants = [variant]
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
        {
                Path.Join(s_workspacePath, "tooth.json"),
                new MockFileData(packageManifest.ToJsonBytes())
        }
        }, s_workspacePath);

        Mock<ILogger> logger = new();

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var pathManager = new PathManager(fileSystem, s_workspacePath, s_workspacePath);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var packService = new PackService(context.Object, packageManager, pathManager);

        // Act & assert.
        await Assert.ThrowsAsync<NotImplementedException>(() => packService.Pack("output"));
    }

    [Fact]
    public async Task Pack_DryRun_NotRunsCommandsNorCreatesFile()
    {
        // Arrange.
        var scripts = CreateEmptyScripts() with { PrePack = ["echo 'pre-pack script'"] };
        var variant = CreateDefaultVariant() with { Scripts = scripts };

        PackageManifest packageManifest = new()
        {
            FormatVersion = PackageManifest.DefaultFormatVersion,
            FormatUuid = PackageManifest.DefaultFormatUuid,
            ToothPath = "example.com/pkg",
            Version = SemVersion.Parse("1.0.0"),
            Info = new PackageManifest.InfoType
            {
                Name = "",
                Description = "",
                Tags = [],
                AvatarUrl = ""
            },
            Variants = [variant]
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
        {
                Path.Join(s_workspacePath, "tooth.json"),
                new MockFileData(packageManifest.ToJsonBytes())
        },
        }, s_workspacePath);

        Mock<ICommandRunner> commandRunner = new();
        commandRunner.Setup(c => c.Run(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        Mock<ILogger> logger = new();

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var pathManager = new PathManager(fileSystem, s_workspacePath, s_workspacePath);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var packService = new PackService(context.Object, packageManager, pathManager);

        // Act.
        await packService.Pack("output", dryRun: true);

        // Assert.
        Assert.False(fileSystem.File.Exists("output"));
        commandRunner.Verify(c => c.Run(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Pack_IgnoreScripts_NotRunsCommands()
    {
        // Arrange.
        var scripts = CreateEmptyScripts() with { PrePack = ["echo 'pre-pack script'"] };
        var variant = CreateDefaultVariant() with { Scripts = scripts };

        PackageManifest packageManifest = new()
        {
            FormatVersion = PackageManifest.DefaultFormatVersion,
            FormatUuid = PackageManifest.DefaultFormatUuid,
            ToothPath = "example.com/pkg",
            Version = SemVersion.Parse("1.0.0"),
            Info = new PackageManifest.InfoType
            {
                Name = "",
                Description = "",
                Tags = [],
                AvatarUrl = ""
            },
            Variants = [variant]
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
        {
                Path.Join(s_workspacePath, "tooth.json"),
                new MockFileData(packageManifest.ToJsonBytes())
        },
        }, s_workspacePath);

        Mock<ICommandRunner> commandRunner = new();
        commandRunner.Setup(c => c.Run(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        Mock<ILogger> logger = new();

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);
        context.SetupGet(c => c.CommandRunner).Returns(commandRunner.Object);
        context.SetupGet(c => c.Logger).Returns(logger.Object);

        var pathManager = new PathManager(fileSystem, s_workspacePath, s_workspacePath);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var packService = new PackService(context.Object, packageManager, pathManager);

        // Act.
        await packService.Pack("output", ignoreScripts: true);

        // Assert.
        Assert.True(fileSystem.File.Exists("output"));
        Assert.Equal(
            Encoding.UTF8.GetString(packageManifest.ToJsonBytes()),
            ReadArchiveEntryContent(fileSystem, "output", "tooth.json"));
        commandRunner.Verify(c => c.Run(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(PackService.ArchiveFormatType.Zip)]
    [InlineData(PackService.ArchiveFormatType.Tar)]
    [InlineData(PackService.ArchiveFormatType.TarGz)]
    public async Task Pack_DifferentArchiveFormats_CreatesArchive(PackService.ArchiveFormatType archiveFormatType)
    {
        // Arrange.
        PackageManifest packageManifest = new()
        {
            FormatVersion = PackageManifest.DefaultFormatVersion,
            FormatUuid = PackageManifest.DefaultFormatUuid,
            ToothPath = "example.com/pkg",
            Version = SemVersion.Parse("1.0.0"),
            Info = new PackageManifest.InfoType
            {
                Name = "",
                Description = "",
                Tags = [],
                AvatarUrl = ""
            },
            Variants = []
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
        {
                Path.Join(s_workspacePath, "tooth.json"),
                new MockFileData(packageManifest.ToJsonBytes())
        },
        }, s_workspacePath);

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_workspacePath, s_workspacePath);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var packService = new PackService(context.Object, packageManager, pathManager);

        // Act.
        await packService.Pack("output", archiveFormat: archiveFormatType);

        // Assert.
        Assert.True(fileSystem.File.Exists("output"));
        Assert.Equal(
            Encoding.UTF8.GetString(packageManifest.ToJsonBytes()),
            ReadArchiveEntryContent(fileSystem, "output", "tooth.json"));
    }

    [Fact]
    public async Task Pack_InvalidArchiveFormat_ThrowsNotImplementedException()
    {
        // Arrange.
        PackageManifest packageManifest = new()
        {
            FormatVersion = PackageManifest.DefaultFormatVersion,
            FormatUuid = PackageManifest.DefaultFormatUuid,
            ToothPath = "example.com/pkg",
            Version = SemVersion.Parse("1.0.0"),
            Info = new PackageManifest.InfoType
            {
                Name = "",
                Description = "",
                Tags = [],
                AvatarUrl = ""
            },
            Variants = []
        };

        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
        {
                Path.Join(s_workspacePath, "tooth.json"),
                new MockFileData(packageManifest.ToJsonBytes())
        },
        }, s_workspacePath);

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, s_workspacePath, s_workspacePath);
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var packService = new PackService(context.Object, packageManager, pathManager);

        // Act & assert.
        await Assert.ThrowsAsync<NotImplementedException>(() => packService.Pack(
            "output",
            archiveFormat: (PackService.ArchiveFormatType)int.MaxValue));
    }

    private static string? ReadArchiveEntryContent(
        MockFileSystem fileSystem,
        string archiveFileRelativePath,
        string entryName)
    {
        using Stream archiveFileStream = fileSystem.File.OpenRead(archiveFileRelativePath);

        using IReader reader = ReaderFactory.Open(archiveFileStream);

        while (reader.MoveToNextEntry())
        {
            if (reader.Entry.Key == entryName)
            {
                using MemoryStream memoryStream = new();

                reader.WriteEntryTo(memoryStream);

                memoryStream.Position = 0;

                using StreamReader streamReader = new(memoryStream);

                return streamReader.ReadToEnd();
            }
        }

        return null;
    }
}