using System.Text;
using System.Text.Json;

namespace Lip.Tests;

public class PackageManifestTests
{
    [Fact]
    public void FromBytes_MinimalInput_Parsed()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": "1.0.0"
            }
            """u8.ToArray();

        var manifest = PackageManifest.FromBytes(bytes);

        Assert.NotNull(manifest);
        Assert.Equal(3, manifest.FormatVersion);
        Assert.Equal("289f771f-2c9a-4d73-9f3f-8492495a924d", manifest.FormatUuid);
        Assert.Equal("test-tooth", manifest.Tooth);
        Assert.Equal("1.0.0", manifest.Version);
    }

    [Fact]
    public void FromBytes_FullInput_Parsed()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": "1.0.0",
                "info": {
                    "name": "Test Package",
                    "description": "A test package",
                    "author": "Test Author",
                    "tags": ["test", "example:tag"],
                    "avatar_url": "https://example.com/avatar.png"
                },
                "variants": [
                    {
                        "platform": "windows",
                        "dependencies": {
                            "some/dependency": "^1.0.0"
                        },
                        "prerequisites": {
                            "some/prerequisite": "^2.0.0"
                        },
                        "assets": [
                            {
                                "type": "zip",
                                "urls": ["https://example.com/asset.zip"],
                                "place": [
                                    {
                                        "type": "dir",
                                        "src": "src",
                                        "dest": "dest"
                                    }
                                ],
                                "preserve": ["*.txt"],
                                "remove": ["*.tmp"]
                            }
                        ],
                        "scripts": {
                            "pre_install": ["echo pre-install"],
                            "install": ["echo install"],
                            "post_install": ["echo post-install"],
                            "pre_pack": ["echo pre-pack"],
                            "post_pack": ["echo post-pack"],
                            "pre_uninstall": ["echo pre-uninstall"],
                            "uninstall": ["echo uninstall"],
                            "post_uninstall": ["echo post-uninstall"],
                            "custom_script": ["echo custom"]
                        }
                    }
                ]
            }
            """u8.ToArray();

        var manifest = PackageManifest.FromBytes(bytes);

        Assert.NotNull(manifest);
        Assert.Equal(3, manifest.FormatVersion);
        Assert.Equal("289f771f-2c9a-4d73-9f3f-8492495a924d", manifest.FormatUuid);
        Assert.Equal("test-tooth", manifest.Tooth);
        Assert.Equal("1.0.0", manifest.Version);

        Assert.NotNull(manifest.Info);
        Assert.Equal("Test Package", manifest.Info.Name);
        Assert.Equal("A test package", manifest.Info.Description);
        Assert.Equal("Test Author", manifest.Info.Author);
        Assert.Equal(new[] { "test", "example:tag" }, manifest.Info.Tags);
        Assert.Equal("https://example.com/avatar.png", manifest.Info.AvatarUrl);

        Assert.NotNull(manifest.Variants);
        Assert.Single(manifest.Variants);
        PackageManifest.VariantType variant = manifest.Variants[0];
        Assert.Equal("windows", variant.Platform);

        Assert.NotNull(variant.Dependencies);
        Assert.Equal("^1.0.0", variant.Dependencies["some/dependency"]);

        Assert.NotNull(variant.Prerequisites);
        Assert.Equal("^2.0.0", variant.Prerequisites["some/prerequisite"]);

        Assert.NotNull(variant.Assets);
        Assert.Single(variant.Assets);
        PackageManifest.AssetType asset = variant.Assets[0];
        Assert.Equal(PackageManifest.AssetType.TypeEnum.Zip, asset.Type);
        Assert.Equal(new[] { "https://example.com/asset.zip" }, asset.Urls);

        Assert.NotNull(asset.Place);
        Assert.Single(asset.Place);
        PackageManifest.PlaceType place = asset.Place[0];
        Assert.Equal(PackageManifest.PlaceType.TypeEnum.Dir, place.Type);
        Assert.Equal("src", place.Src);
        Assert.Equal("dest", place.Dest);

        Assert.Equal(new[] { "*.txt" }, asset.Preserve);
        Assert.Equal(new[] { "*.tmp" }, asset.Remove);

        Assert.NotNull(variant.Scripts);
        Assert.Equal(new[] { "echo pre-install" }, variant.Scripts.PreInstall);
        Assert.Equal(new[] { "echo install" }, variant.Scripts.Install);
        Assert.Equal(new[] { "echo post-install" }, variant.Scripts.PostInstall);
        Assert.Equal(new[] { "echo pre-pack" }, variant.Scripts.PrePack);
        Assert.Equal(new[] { "echo post-pack" }, variant.Scripts.PostPack);
        Assert.Equal(new[] { "echo pre-uninstall" }, variant.Scripts.PreUninstall);
        Assert.Equal(new[] { "echo uninstall" }, variant.Scripts.Uninstall);
        Assert.Equal(new[] { "echo post-uninstall" }, variant.Scripts.PostUninstall);
        Assert.Equal(new[] { "echo custom" }, variant.Scripts.AdditionalScripts["custom_script"]);
    }

    [Fact]
    public void FromBytes_InvalidJson_ThrowsJsonException()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("invalid-json");

        Assert.Throws<JsonException>(() => PackageManifest.FromBytes(bytes));
    }

    [Fact]
    public void FromBytes_MissingRequiredField_ThrowsArgumentException()
    {
        byte[] bytes = """
            {
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": "1.0.0"
            }
            """u8.ToArray();

        Assert.Throws<JsonException>(() => PackageManifest.FromBytes(bytes));
    }

    [Fact]
    public void FromBytes_InvalidFieldType_ThrowsJsonException()
    {
        byte[] bytes = """
            {
                "format_version": "3",
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": 1
            }
            """u8.ToArray();

        Assert.Throws<JsonException>(() => PackageManifest.FromBytes(bytes));
    }

    [Fact]
    public void FromBytes_AdditionalField_Parsed()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": "1.0.0",
                "additional_field": "additional-value"
            }
            """u8.ToArray();

        var manifest = PackageManifest.FromBytes(bytes);

        Assert.NotNull(manifest);
        Assert.Equal(3, manifest.FormatVersion);
        Assert.Equal("289f771f-2c9a-4d73-9f3f-8492495a924d", manifest.FormatUuid);
        Assert.Equal("test-tooth", manifest.Tooth);
        Assert.Equal("1.0.0", manifest.Version);
    }

    [Fact]
    public void FromBytes_InvalidFormatVersion_ThrowsArgumentException()
    {
        byte[] bytes = Encoding.UTF8.GetBytes($$"""
            {
                "format_version": 0,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": "1.0.0"
            }
            """);

        Assert.Throws<ArgumentException>(() => PackageManifest.FromBytes(bytes));
    }

    [Fact]
    public void FromBytes_InvalidFormatUuid_ThrowsArgumentException()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "invalid-uuid",
                "tooth": "test-tooth",
                "version": "1.0.0"
            }
            """u8.ToArray();

        Assert.Throws<ArgumentException>(() => PackageManifest.FromBytes(bytes));
    }

    [Fact]
    public void FromBytes_InvalidVersion_ThrowsArgumentException()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": "invalid-version"
            }
            """u8.ToArray();

        Assert.Throws<ArgumentException>(() => PackageManifest.FromBytes(bytes));
    }

    [Fact]
    public void FromBytes_InvalidTag_ThrowsJsonException()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": "1.0.0",
                "info": {
                    "tags": ["invalid.tag"],
                }
            }
            """u8.ToArray();

        Assert.Throws<ArgumentException>(() => PackageManifest.FromBytes(bytes));
    }

    [Fact]
    public void FromBytes_ValidAdditionalScript_Parsed()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": "1.0.0",
                "variants": [
                    {
                        "platform": "windows",
                        "scripts": {
                            "pre_install": ["echo pre-install"],
                            "custom_script": ["echo custom"]
                        }
                    }
                ]
            }
            """u8.ToArray();

        var manifest = PackageManifest.FromBytes(bytes);

        Assert.NotNull(manifest);
        Assert.Equal(3, manifest.FormatVersion);
        Assert.Equal("289f771f-2c9a-4d73-9f3f-8492495a924d", manifest.FormatUuid);
        Assert.Equal("test-tooth", manifest.Tooth);
        Assert.Equal("1.0.0", manifest.Version);

        Assert.NotNull(manifest.Variants);
        Assert.Single(manifest.Variants);
        PackageManifest.VariantType variant = manifest.Variants[0];
        Assert.Equal("windows", variant.Platform);

        Assert.NotNull(variant.Scripts);
        Assert.Equal(new[] { "echo pre-install" }, variant.Scripts.PreInstall);
        Assert.Equal(new[] { "echo custom" }, variant.Scripts.AdditionalScripts["custom_script"]);
    }

    [Fact]
    public void FromBytes_InvalidAdditionalScriptKey_ThrowsJsonException()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": "1.0.0",
                "variants": [
                    {
                        "platform": "windows",
                        "scripts": {
                            "pre_install": ["echo pre-install"],
                            "invalid-script": ["echo invalid"]
                        }
                    }
                ]
            }
            """u8.ToArray();

        Assert.Throws<JsonException>(() => PackageManifest.FromBytes(bytes));
    }

    [Theory]
    [InlineData("\"echo valid\"")]
    [InlineData("[0]")]
    public void FromBytes_InvalidAdditionalScriptValue_ThrowsJsonException(object invalidScript)
    {
        byte[] bytes = Encoding.UTF8.GetBytes($$"""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "test-tooth",
                "version": "1.0.0",
                "variants": [
                    {
                        "platform": "windows",
                        "scripts": {
                            "pre_install": ["echo pre-install"],
                            "invalid_script": {{invalidScript}}
                        }
                    }
                ]
            }
            """);

        Assert.Throws<JsonException>(() => PackageManifest.FromBytes(bytes));
    }
}
