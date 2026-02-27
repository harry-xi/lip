using Flurl.Http.Testing;
using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using Moq;
using Semver;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Lip.Core.Tests.PackageRegistries;

public class LiprPackageRegistryTests
{
    [Fact]
    public async Task GetAvailableVersions_ReturnsSortedVersionsFromIndex()
    {
        using HttpTest httpTest = new();
        var mockDownloader = new Mock<IFileDownloader>();
        var mockCache = new Mock<ICacheService>();
        LiprPackageRegistry registry = new(mockDownloader.Object, mockCache.Object);

        httpTest.RespondWith("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "packages": {
                    "github.com/LiteLDev/LeviLamina": {
                        "info": {
                            "name": "LeviLamina",
                            "description": "A lightweight, modular and versatile mod loader for Minecraft Bedrock Edition.",
                            "tags": [],
                            "avatar_url": "https://lamina.levimc.org/logo.svg"
                        },
                        "updated_at": "2026-02-26T15:57:48Z",
                        "stars": 1509,
                        "versions": {
                            "1.9.2": ["", "client"],
                            "1.8.0-rc.2": ["", "client"],
                            "1.9.0": ["", "client"],
                            "1.7.7": [""]
                        }
                    }
                }
            }
            """);

        List<SemVersion> versions = (await registry.GetAvailableVersions(PackageId.Parse("github.com/LiteLDev/LeviLamina"))).ToList();

        Assert.Equal(4, versions.Count);
        Assert.Equal(SemVersion.Parse("1.7.7", SemVersionStyles.Any), versions[0]);
        Assert.Equal(SemVersion.Parse("1.8.0-rc.2", SemVersionStyles.Any), versions[1]);
        Assert.Equal(SemVersion.Parse("1.9.0", SemVersionStyles.Any), versions[2]);
        Assert.Equal(SemVersion.Parse("1.9.2", SemVersionStyles.Any), versions[3]);
    }

    [Fact]
    public async Task GetPackageManifest_ReturnsDeserializedManifest()
    {
        // Arrange
        var mockDownloader = new Mock<IFileDownloader>();
        var mockCache = new Mock<ICacheService>();
        LiprPackageRegistry registry = new(mockDownloader.Object, mockCache.Object);

        PackageId pkgId = new("github.com/LiteLDev/LeviLamina", "");
        SemVersion version = new(1, 9, 2);
        PackageSpec pkgSpec = new(pkgId, version);

        string manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "github.com/LiteLDev/LeviLamina",
                "version": "1.9.2",
                "info": {
                    "name": "LeviLamina",
                    "description": "A lightweight, modular and versatile mod loader for Minecraft Bedrock Edition.",
                    "tags": [],
                    "avatar_url": "https://lamina.levimc.org/logo.svg"
                },
                "variants": [
                    {
                        "platform": "win-x64",
                        "dependencies": {
                            "github.com/LiteLDev/bds": "1.21.132-patch.3",
                            "github.com/LiteLDev/CrashLogger": "1.3.*",
                            "github.com/LiteLDev/PreLoader": "1.15.7"
                        },
                        "assets": [
                            {
                                "type": "zip",
                                "urls": [
                                    "https://{{tooth}}/releases/download/v{{version}}/levilamina-v{{version}}-server-release-windows-x64.zip"
                                ],
                                "placements": [
                                    {
                                        "type": "dir",
                                        "src": "LeviLamina/",
                                        "dest": "plugins/LeviLamina/"
                                    }
                                ]
                            }
                        ],
                        "remove_files": [
                            "bedrock_server_mod.exe"
                        ],
                        "scripts": {
                            "post_install": [
                                ".\\PeEditor.exe -mb"
                            ]
                        }
                    },
                    {
                        "label": "client",
                        "platform": "win-x64",
                        "dependencies": {
                            "github.com/LiteLDev/CrashLogger#client": "1.3.*",
                            "github.com/LiteLDev/PreLoader": "1.15.7"
                        }
                    }
                ]
            }
            """;

        var mockFileSystem = new MockFileSystem();
        var mockFile = new Mock<IFileInfo>();

        // Setup OpenRead to return a stream from the mock file system
        var path = @"C:\fake\path\tooth.json";
        mockFileSystem.AddFile(path, new MockFileData(manifestJson));
        mockFile.Setup(f => f.OpenRead())
            .Returns(mockFileSystem.File.OpenRead(path));

        mockCache.Setup(c => c.GetOrCreateFile(It.IsAny<string>(), It.IsAny<Func<IFileInfo, Task>>()))
            .Callback<string, Func<IFileInfo, Task>>((key, factory) => factory(mockFile.Object))
            .ReturnsAsync(mockFile.Object);

        // Act
        PackageManifest result = await registry.GetPackageManifest(pkgSpec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("github.com/LiteLDev/LeviLamina", result.Path);
        Assert.Equal(version, result.Version);
        Assert.Equal(2, result.Variants.Count);
        Assert.Equal("client", result.Variants[1].Label);
        Assert.Equal(
            SemVersionRange.Parse("1.3.*"),
            result.Variants[1].Dependencies[PackageId.Parse("github.com/LiteLDev/CrashLogger#client")]);

        // Verify that GetOrCreateFile was called with the correct URL
        string expectedUrl = $"https://lipr.levimc.org/{pkgId.Path}/@v/{version}/tooth.json";
        mockCache.Verify(c => c.GetOrCreateFile(
            expectedUrl,
            It.IsAny<Func<IFileInfo, Task>>()),
            Times.Once);
    }
}