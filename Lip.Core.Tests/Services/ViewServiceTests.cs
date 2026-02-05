using Moq;
using System.Runtime.InteropServices;

namespace Lip.Core.Tests;

using Lip.Core;
using Lip.Core.PackageRegistries;
using Lip.Core.Services;
using System.Text.Json;

public class ViewServiceTests
{
    private static readonly string s_packageManifestData = $$"""
        {
            "format_version": 3,
            "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
            "tooth": "example.com/repo",
            "version": "1.0.0",
            "info": {
                "name": "",
                "description": "",
                "tags": [],
                "avatar_url": ""
            },
            "variants": [
                {
                    "label": "",
                    "platform": "{{RuntimeInformation.RuntimeIdentifier}}",
                    "dependencies": {},
                    "assets": [
                        {
                            "type": "self",
                            "urls": [],
                            "placements": []
                        },
                        {
                            "type": "zip",
                            "urls": [],
                            "placements": []
                        },
                        {
                            "type": "zip",
                            "urls": [
                                "https://example.com/test.file"
                            ],
                            "placements": []
                        }
                    ],
                    "preserve_files": [],
                    "remove_files": [],
                    "scripts": {
                        "pre_install": [],
                        "install": [],
                        "post_install": [],
                        "pre_uninstall": [],
                        "uninstall": [],
                        "post_uninstall": []
                    }
                }
            ]
        }
        """.ReplaceLineEndings();

    [Fact]
    public async Task View_ReturnsFullManifest()
    {
        // Arrange.
        Mock<IPackageRegistry> packageRegistryMock = new();
        PackageManifest manifest = PackageManifest.Create(JsonDocument.Parse(s_packageManifestData).RootElement);
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(manifest);

        ViewService viewService = new ViewService(packageRegistryMock.Object);

        // Act.
        string result = await viewService.View("example.com/repo@1.0.0");

        // Assert.
        Assert.True(JsonElement.DeepEquals(JsonDocument.Parse(s_packageManifestData).RootElement, JsonDocument.Parse(result).RootElement));
    }
}