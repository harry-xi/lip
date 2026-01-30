using Moq;
using Semver;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;

namespace Lip.Core.Tests;

public class ListServiceTests
{


    [Fact]
    public async Task List_ReturnsListItems()
    {
        // Arrange.
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "tooth_lock.json", new MockFileData($$"""
            {
                "format_version": {{PackageLock.DefaultFormatVersion}},
                "format_uuid": "{{PackageLock.DefaultFormatUuid}}",
                "packages": [
                    {
                        "locked": true,
                        "manifest": {
                            "format_version": {{PackageManifest.DefaultFormatVersion}},
                            "format_uuid": "{{PackageManifest.DefaultFormatUuid}}",
                            "tooth": "example.com/pkg1",
                            "version": "1.0.0",
                            "variants": [
                                {
                                    "label": "variant1",
                                    "platform": "{{RuntimeInformation.RuntimeIdentifier}}"
                                }
                            ]
                        },
                        "variant": "variant1",
                        "files": []
                    },
                    {
                        "locked": false,
                        "manifest": {
                            "format_version": {{PackageManifest.DefaultFormatVersion}},
                            "format_uuid": "{{PackageManifest.DefaultFormatUuid}}",
                            "tooth": "example.com/pkg2",
                            "version": "1.0.1",
                            "variants": [
                                {
                                    "label": "variant2",
                                    "platform": "{{RuntimeInformation.RuntimeIdentifier}}"
                                }
                            ]
                        },
                        "variant": "variant2",
                        "files": []
                    }
                ]
            }
            """) }
        });

        Mock<IContext> context = new();
        context.SetupGet(c => c.FileSystem).Returns(fileSystem);

        var pathManager = new PathManager(fileSystem, "/", "/");
        var cacheManager = new Mock<ICacheManager>();
        var packageManager = new PackageManager(context.Object, cacheManager.Object, pathManager);
        var listService = new Services.ListService(packageManager);

        // Act.
        var listItems = await listService.List();

        // Assert.
        Assert.Equal(2, listItems.Count);

        Assert.Equal("example.com/pkg1", listItems[0].Specifier.ToothPath);
        Assert.Equal("1.0.0", listItems[0].Specifier.Version.ToString());
        Assert.Equal("variant1", listItems[0].Variant.Label);
        Assert.True(listItems[0].Locked);

        Assert.Equal("example.com/pkg2", listItems[1].Specifier.ToothPath);
        Assert.Equal("1.0.1", listItems[1].Specifier.Version.ToString());
        Assert.Equal("variant2", listItems[1].Variant.Label);
        Assert.False(listItems[1].Locked);
    }
}