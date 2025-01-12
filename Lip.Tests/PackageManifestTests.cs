using System.Text;
using System.Text.Json;

namespace Lip.Tests;

public class PackageManifestTests
{
    public class AssetTypeTests
    {
        [Fact]
        public void Deserialize_MinimumJson_Passes()
        {
            // Arrange.
            string json = """
            {
                "type": "self"
            }
            """;

            // Act.
            PackageManifest.AssetType? asset = JsonSerializer.Deserialize<PackageManifest.AssetType>(json);

            // Assert.
            Assert.NotNull(asset);
            Assert.Equal(PackageManifest.AssetType.TypeEnum.Self, asset.Type);
            Assert.Null(asset.Urls);
            Assert.Null(asset.Place);
            Assert.Null(asset.Preserve);
            Assert.Null(asset.Remove);
        }

        [Fact]
        public void Deserialize_MaximumJson_Passes()
        {
            // Arrange.
            string json = """
            {
                "type": "self",
                "urls": [],
                "place": [],
                "preserve": [],
                "remove": []
            }
            """;

            // Act.
            PackageManifest.AssetType? asset = JsonSerializer.Deserialize<PackageManifest.AssetType>(json);

            // Assert.
            Assert.NotNull(asset);
            Assert.Equal(PackageManifest.AssetType.TypeEnum.Self, asset.Type);
            Assert.Equal([], asset.Urls);
            Assert.Equal([], asset.Place);
            Assert.Equal([], asset.Preserve);
            Assert.Equal([], asset.Remove);
        }
    }

    public class InfoTypeTests
    {
        [Fact]
        public void Deserialize_MinimumJson_Passes()
        {
            // Arrange.
            string json = "{}";

            // Act.
            PackageManifest.InfoType? info = JsonSerializer.Deserialize<PackageManifest.InfoType>(json);

            // Assert.
            Assert.NotNull(info);
            Assert.Null(info.Name);
            Assert.Null(info.Description);
            Assert.Null(info.Author);
            Assert.Null(info.Tags);
            Assert.Null(info.AvatarUrl);
        }

        [Fact]
        public void Deserialize_MaximumJson_Passes()
        {
            // Arrange.
            string json = """
            {
                "name": "",
                "description": "",
                "author": "",
                "tags": ["tag", "tag:subtag"],
                "avatar_url": ""
            }
            """;

            // Act.
            PackageManifest.InfoType? info = JsonSerializer.Deserialize<PackageManifest.InfoType>(json);

            // Assert.
            Assert.NotNull(info);
            Assert.Equal("", info.Name);
            Assert.Equal("", info.Description);
            Assert.Equal("", info.Author);
            Assert.Equal(new[] { "tag", "tag:subtag" }, info.Tags);
            Assert.Equal("", info.AvatarUrl);
        }

        [Theory]
        [InlineData("invalid.tag")]
        [InlineData("tag:invalid.subtag")]
        [InlineData("invalid.tag:subtag")]
        [InlineData("invalid-tag:")]
        [InlineData(":invalid-subtag")]
        [InlineData(":")]
        [InlineData("")]
        public void Deserialize_InvalidTag_ThrowsArgumentException(string tag)
        {
            // Arrange.
            string json = $$"""
            {
                "tags": ["{{tag}}"]
            }
            """;

            // Act.
            ArgumentException exception = Assert.Throws<ArgumentException>(
                "value", () => JsonSerializer.Deserialize<PackageManifest.InfoType>(json));

            // Assert.
            Assert.Equal($"Tag {tag} is invalid. (Parameter 'value')", exception.Message);
        }
    }

    public class PlaceTypeTests
    {
        [Fact]
        public void Deserialize_CommonInput_Passes()
        {
            // Arrange.
            string json = """
            {
                "type": "file",
                "src": "",
                "dest": ""
            }
            """;

            // Act.
            PackageManifest.PlaceType? place = JsonSerializer.Deserialize<PackageManifest.PlaceType>(json);

            // Assert.
            Assert.NotNull(place);
            Assert.Equal(PackageManifest.PlaceType.TypeEnum.File, place.Type);
            Assert.Equal("", place.Src);
            Assert.Equal("", place.Dest);
        }
    }

    public class ScriptsTypeTests
    {
        [Theory]
        [InlineData("script")]
        [InlineData("additional_script")]
        public void Constructor_AdditionalScriptsInitialized_Passes(string scriptName)
        {
            // Arrange.
            Dictionary<string, List<string>> additionalScripts = new()
            {
                [scriptName] = ["echo additional"]
            };

            // Act.
            var scripts = new PackageManifest.ScriptsType
            {
                AdditionalScripts = additionalScripts
            };

            // Assert.
            Assert.NotNull(scripts.AdditionalProperties);
            Assert.Single(scripts.AdditionalProperties);
            Assert.Single(scripts.AdditionalScripts);
            Assert.Equal(new[] { "echo additional" }, scripts.AdditionalScripts[scriptName]);
        }

        [Theory]
        [InlineData("invalid-script")]
        [InlineData("invalid.script")]
        [InlineData("invalid script")]
        [InlineData("invalidScript")]
        public void Constructor_InvalidAdditionalScriptName_ThrowsArgumentException(string scriptName)
        {
            // Arrange.
            Dictionary<string, List<string>> additionalScripts = new()
            {
                [scriptName] = ["echo invalid"]
            };

            // Act.
            ArgumentException exception = Assert.Throws<ArgumentException>(
                "value", () => new PackageManifest.ScriptsType { AdditionalScripts = additionalScripts });

            // Assert.
            Assert.Equal($"Script name {scriptName} is invalid. (Parameter 'value')", exception.Message);
        }

        [Fact]
        public void Deserialize_MinimumJson_Passes()
        {
            // Arrange.
            string json = "{}";

            // Act.
            PackageManifest.ScriptsType? scripts = JsonSerializer.Deserialize<PackageManifest.ScriptsType>(json);

            // Assert.
            Assert.NotNull(scripts);
            Assert.Null(scripts.PreInstall);
            Assert.Null(scripts.Install);
            Assert.Null(scripts.PostInstall);
            Assert.Null(scripts.PrePack);
            Assert.Null(scripts.PostPack);
            Assert.Null(scripts.PreUninstall);
            Assert.Null(scripts.Uninstall);
            Assert.Null(scripts.PostUninstall);
            Assert.Null(scripts.AdditionalProperties);
            Assert.Equal([], scripts.AdditionalScripts);
        }

        [Fact]
        public void Deserialize_MaximumJson_Passes()
        {
            // Arrange.
            string json = """
            {
                "pre_install": [],
                "install": [],
                "post_install": [],
                "pre_pack": [],
                "post_pack": [],
                "pre_uninstall": [],
                "uninstall": [],
                "post_uninstall": [],
                "additional_script": [
                    "echo additional"
                ]
            }
            """;

            // Act.
            PackageManifest.ScriptsType? scripts = JsonSerializer.Deserialize<PackageManifest.ScriptsType>(json);

            // Assert.
            Assert.NotNull(scripts);
            Assert.Equal([], scripts.PreInstall);
            Assert.Equal([], scripts.Install);
            Assert.Equal([], scripts.PostInstall);
            Assert.Equal([], scripts.PrePack);
            Assert.Equal([], scripts.PostPack);
            Assert.Equal([], scripts.PreUninstall);
            Assert.Equal([], scripts.Uninstall);
            Assert.Equal([], scripts.PostUninstall);
            Assert.NotNull(scripts.AdditionalProperties);
            Assert.Single(scripts.AdditionalProperties);
            Assert.Contains("additional_script", scripts.AdditionalProperties);
            Assert.Single(scripts.AdditionalScripts);
            Assert.Contains("additional_script", scripts.AdditionalScripts);
            Assert.Equal(["echo additional"], scripts.AdditionalScripts["additional_script"]);
        }

        [Theory]
        [InlineData("invalid-script")]
        [InlineData("invalid.script")]
        [InlineData("invalid script")]
        [InlineData("invalidScript")]
        public void Deserialize_InvalidPropertyKey_Passes(string scriptName)
        {
            // Arrange.
            string json = $$"""
            {
                "{{scriptName}}": []
            }
            """;

            // Act.
            PackageManifest.ScriptsType? scripts = JsonSerializer.Deserialize<PackageManifest.ScriptsType>(json);

            // Assert.
            Assert.NotNull(scripts);
            Assert.NotNull(scripts.AdditionalProperties);
            Assert.Single(scripts.AdditionalProperties);
            Assert.Equal([], scripts.AdditionalScripts);
        }

        [Fact]
        public void Deserialize_InvalidPropertyValueKind_Passes()
        {
            // Arrange.
            string json = """
            {
                "additional_script_1": null
            }
            """;

            // Act.
            PackageManifest.ScriptsType? scripts = JsonSerializer.Deserialize<PackageManifest.ScriptsType>(json);

            // Assert.
            Assert.NotNull(scripts);
            Assert.NotNull(scripts.AdditionalProperties);
            Assert.Single(scripts.AdditionalProperties);
            Assert.Equal([], scripts.AdditionalScripts);
        }

        [Fact]
        public void Deserialize_InvalidPropertyItemValueKind_Passes()
        {
            // Arrange.
            string json = """
            {
                "additional_script": [
                    null
                ]
            }
            """;

            // Act.
            PackageManifest.ScriptsType? scripts = JsonSerializer.Deserialize<PackageManifest.ScriptsType>(json);

            // Assert.
            Assert.NotNull(scripts);
            Assert.NotNull(scripts.AdditionalProperties);
            Assert.Single(scripts.AdditionalProperties);
            Assert.Equal([], scripts.AdditionalScripts);
        }
    }

    public class VariantTypeTests
    {
        [Fact]
        public void Deserialize_MinimumJson_Passes()
        {
            // Arrange.
            string json = "{}";

            // Act.
            PackageManifest.VariantType? variant = JsonSerializer.Deserialize<PackageManifest.VariantType>(json);

            // Assert.
            Assert.NotNull(variant);
            Assert.Null(variant.Platform);
            Assert.Null(variant.Dependencies);
            Assert.Null(variant.Assets);
            Assert.Null(variant.Scripts);
        }

        [Fact]
        public void Deserialize_MaximumJson_Passes()
        {
            // Arrange.
            string json = """
            {
                "label": "",
                "platform": "",
                "dependencies": {},
                "assets": [],
                "scripts": {}
            }
            """;

            // Act.
            PackageManifest.VariantType? variant = JsonSerializer.Deserialize<PackageManifest.VariantType>(json);

            // Assert.
            Assert.NotNull(variant);
            Assert.Equal("", variant.Label);
            Assert.Equal("", variant.Platform);
            Assert.Equal([], variant.Dependencies);
            Assert.Equal([], variant.Assets);
            Assert.NotNull(variant.Scripts);
        }
    }

    [Fact]
    public void FromBytes_MinimumJson_Passes()
    {
        // Arrange.
        byte[] bytes = Encoding.UTF8.GetBytes("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "",
                "version": "1.0.0"
            }
            """);

        // Act.
        var manifest = PackageManifest.FromBytes(bytes);

        // Assert.
        Assert.Equal(3, manifest.FormatVersion);
        Assert.Equal("289f771f-2c9a-4d73-9f3f-8492495a924d", manifest.FormatUuid);
        Assert.Equal("", manifest.Tooth);
        Assert.Equal("1.0.0", manifest.Version);
    }

    [Fact]
    public void FromBytes_MaximumJson_Passes()
    {
        // Arrange.
        byte[] bytes = Encoding.UTF8.GetBytes("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "",
                "version": "1.0.0",
                "info": {},
                "variants": []
            }
            """);

        // Act.
        var manifest = PackageManifest.FromBytes(bytes);

        // Assert.
        Assert.Equal(3, manifest.FormatVersion);
        Assert.Equal("289f771f-2c9a-4d73-9f3f-8492495a924d", manifest.FormatUuid);
        Assert.Equal("", manifest.Tooth);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.NotNull(manifest.Info);
        Assert.NotNull(manifest.Variants);
        Assert.Equal([], manifest.Variants);
    }

    [Fact]
    public void FromBytes_NullJson_ThrowsArgumentException()
    {
        // Arrange.
        byte[] bytes = Encoding.UTF8.GetBytes("null");

        // Act.
        ArgumentException exception = Assert.Throws<ArgumentException>("bytes", () => PackageManifest.FromBytes(bytes));

        // Assert.
        Assert.Equal("Failed to deserialize package manifest. (Parameter 'bytes')", exception.Message);
    }

    [Fact]
    public void FromBytes_InvalidFormatVersion_ThrowsArgumentException()
    {
        // Arrange.
        byte[] bytes = Encoding.UTF8.GetBytes("""
            {
                "format_version": 0,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "",
                "version": "1.0.0"
            }
            """);

        // Act.
        ArgumentException exception = Assert.Throws<ArgumentException>("value", () => PackageManifest.FromBytes(bytes));

        // Assert.
        Assert.Equal("Format version '0' is not equal to 3. (Parameter 'value')", exception.Message);
    }

    [Fact]
    public void FromBytes_InvalidFormatUuid_ThrowsArgumentException()
    {
        // Arrange.
        byte[] bytes = Encoding.UTF8.GetBytes("""
            {
                "format_version": 3,
                "format_uuid": "invalid-uuid",
                "tooth": "",
                "version": "1.0.0"
            }
            """);

        // Act.
        ArgumentException exception = Assert.Throws<ArgumentException>("value", () => PackageManifest.FromBytes(bytes));

        // Assert.
        Assert.Equal("Format UUID 'invalid-uuid' is not equal to 289f771f-2c9a-4d73-9f3f-8492495a924d. (Parameter 'value')", exception.Message);
    }

    [Fact]
    public void FromBytes_InvalidVersion_ThrowsArgumentException()
    {
        // Arrange.
        byte[] bytes = Encoding.UTF8.GetBytes("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "",
                "version": "0.0.0.0"
            }
            """);

        // Act.
        ArgumentException exception = Assert.Throws<ArgumentException>("value", () => PackageManifest.FromBytes(bytes));

        // Assert.
        Assert.Equal("Version '0.0.0.0' is invalid. (Parameter 'value')", exception.Message);
    }

    [Fact]
    public void ToBytes_MinimumJson_Passes()
    {
        // Arrange.
        var manifest = new PackageManifest
        {
            FormatVersion = 3,
            FormatUuid = "289f771f-2c9a-4d73-9f3f-8492495a924d",
            Tooth = "",
            Version = "1.0.0"
        };

        // Act.
        byte[] bytes = manifest.ToBytes();

        // Assert.
        Assert.Equal("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "",
                "version": "1.0.0"
            }
            """.ReplaceLineEndings(), Encoding.UTF8.GetString(bytes).ReplaceLineEndings());
    }
}
