using Moq;
using System.Runtime.InteropServices;

namespace Lip.Core.Tests;

using Lip.Core;
using Lip.Core.PackageRegistries; // For IPackageRegistry
using Lip.Core.Services;
using System.Text.Json;

public class ViewServiceTests
{
    private static readonly string s_cacheDir = OperatingSystem.IsWindows()
        ? Path.Join("C:", "path", "to", "cache")
        : Path.Join("/", "path", "to", "cache");

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
                        "pre_pack": [],
                        "post_pack": [],
                        "pre_uninstall": [],
                        "uninstall": [],
                        "post_uninstall": []
                    }
                }
            ]
        }
        """.ReplaceLineEndings();

    [Fact]
    public void ViewArgs_Constructor_TrivialValues_Passes()
    {
        // Arrange.
        ViewService.Args viewArgs = new();

        // Act.
        viewArgs = viewArgs with { };
    }

    [Fact]
    public async Task View_EmptyPath_ReturnsFullManifest()
    {
        // Arrange.
        Mock<IPackageRegistry> packageRegistryMock = new();
        PackageManifest manifest = PackageManifest.FromJsonElement(JsonDocument.Parse(s_packageManifestData).RootElement);
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(manifest);

        ViewService viewService = new ViewService(packageRegistryMock.Object);

        // Act.
        string result = await viewService.View("example.com/repo@1.0.0", null, new());

        // Assert.
        Assert.Equal(s_packageManifestData, result);
    }

    [Theory]
    [InlineData("format_version", "3")]
    [InlineData("format_uuid", "289f771f-2c9a-4d73-9f3f-8492495a924d")]
    [InlineData("variants[0].assets[2].urls[0]", "https://example.com/test.file")]
    public async Task View_ValidPath_ReturnsField(string path, string expectedField)
    {
        // Arrange.
        Mock<IPackageRegistry> packageRegistryMock = new();
        PackageManifest manifest = PackageManifest.FromJsonElement(JsonDocument.Parse(s_packageManifestData).RootElement);
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(manifest);

        ViewService viewService = new ViewService(packageRegistryMock.Object);

        // Act.
        string result = await viewService.View("example.com/repo@1.0.0", path, new());

        // Assert.
        Assert.Equal(expectedField, result);
    }

    [Fact]
    public async Task View_ComplexField_ReturnsField()
    {
        // Arrange.
        Mock<IPackageRegistry> packageRegistryMock = new();
        PackageManifest manifest = PackageManifest.FromJsonElement(JsonDocument.Parse(s_packageManifestData).RootElement);
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(manifest);

        ViewService viewService = new ViewService(packageRegistryMock.Object);

        // Act.
        string result = await viewService.View("example.com/repo@1.0.0", "variants[0].assets[0]", new());

        // Assert.
        Assert.Equal(@"{type: ""self"", urls: [], placements: []}", result);
    }

    [Fact]
    public async Task View_UnmatchedToothPath_ThrowsInvalidOperationException()
    {
        // Arrange.
        Mock<IPackageRegistry> packageRegistryMock = new();
        PackageManifest manifest = PackageManifest.FromJsonElement(JsonDocument.Parse(s_packageManifestData).RootElement);
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(manifest);

        ViewService viewService = new ViewService(packageRegistryMock.Object);

        // Act & Assert.
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await viewService.View("example.com/invalid@1.0.0", string.Empty, new()));
    }

    [Fact]
    public async Task View_UnmatchedVersion_ThrowsInvalidOperationException()
    {
        // Arrange.
        Mock<IPackageRegistry> packageRegistryMock = new();
        PackageManifest manifest = PackageManifest.FromJsonElement(JsonDocument.Parse(s_packageManifestData).RootElement);
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(manifest);

        ViewService viewService = new ViewService(packageRegistryMock.Object);

        // Act & Assert.
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await viewService.View("example.com/repo@2.0.0", string.Empty, new()));
    }

    [Fact]
    public async Task View_InvalidPath_ThrowsFormatException()
    {
        // Arrange.
        Mock<IPackageRegistry> packageRegistryMock = new();
        PackageManifest manifest = PackageManifest.FromJsonElement(JsonDocument.Parse(s_packageManifestData).RootElement);
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(manifest);

        ViewService viewService = new ViewService(packageRegistryMock.Object);

        // Act & Assert.
        await Assert.ThrowsAsync<FormatException>(async () => await viewService.View("example.com/repo@1.0.0", "@#$%^", new()));
    }

    [Fact]
    public async Task View_PathNotFound_ReturnsEmpty()
    {
        // Arrange.
        Mock<IPackageRegistry> packageRegistryMock = new();
        PackageManifest manifest = PackageManifest.FromJsonElement(JsonDocument.Parse(s_packageManifestData).RootElement);
        packageRegistryMock.Setup(r => r.GetManifest(It.IsAny<PackageSpecifier>())).ReturnsAsync(manifest);

        ViewService viewService = new ViewService(packageRegistryMock.Object);

        // Act.
        string result = await viewService.View("example.com/repo@1.0.0", "nonexistent", new());

        // Assert.
        Assert.Equal(string.Empty, result);
    }
}