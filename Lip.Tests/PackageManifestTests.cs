using System.Text;
using System.Text.Json;

namespace Lip.Tests;

public class PackageManifest_AssetTypeTests
{
    [Fact]
    public void Deserialize_MinimumJson_Passes()
    {
        string json = """
            {
                "type": "self"
            }
            """;

        PackageManifest.AssetType? asset = JsonSerializer.Deserialize<PackageManifest.AssetType>(json);

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
        string json = """
            {
                "type": "self",
                "urls": [],
                "place": [],
                "preserve": [],
                "remove": []
            }
            """;

        PackageManifest.AssetType? asset = JsonSerializer.Deserialize<PackageManifest.AssetType>(json);

        Assert.NotNull(asset);
        Assert.Equal(PackageManifest.AssetType.TypeEnum.Self, asset.Type);
        Assert.Equal([], asset.Urls);
        Assert.Equal([], asset.Place);
        Assert.Equal([], asset.Preserve);
        Assert.Equal([], asset.Remove);
    }
}

public class PackageManifest_InfoTypeTests
{
    [Fact]
    public void Deserialize_MinimumJson_Passes()
    {
        string json = "{}";

        PackageManifest.InfoType? info = JsonSerializer.Deserialize<PackageManifest.InfoType>(json);

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
        string json = """
            {
                "name": "",
                "description": "",
                "author": "",
                "tags": ["tag", "tag:subtag"],
                "avatar_url": ""
            }
            """;

        PackageManifest.InfoType? info = JsonSerializer.Deserialize<PackageManifest.InfoType>(json);

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
        string json = $$"""
            {
                "tags": ["{{tag}}"]
            }
            """;

        Assert.Throws<ArgumentException>("value", () => JsonSerializer.Deserialize<PackageManifest.InfoType>(json));
    }
}

public class PackageManifest_PlaceTypeTests
{
    [Fact]
    public void Deserialize_CommonInput_Passes()
    {
        string json = """
            {
                "type": "file",
                "src": "",
                "dest": ""
            }
            """;

        PackageManifest.PlaceType? place = JsonSerializer.Deserialize<PackageManifest.PlaceType>(json);

        Assert.NotNull(place);
        Assert.Equal(PackageManifest.PlaceType.TypeEnum.File, place.Type);
        Assert.Equal("", place.Src);
        Assert.Equal("", place.Dest);
    }
}

public class PackageManifest_ScriptsTypeTests
{
    [Fact]
    public void Constructor_AdditionalScriptsInitialized_Passes()
    {
        var scripts = new PackageManifest.ScriptsType
        {
            AdditionalScripts = new Dictionary<string, List<string>>
            {
                ["additional_script"] = ["echo additional"]
            }
        };

        Assert.NotNull(scripts.AdditionalProperties);
        Assert.Single(scripts.AdditionalProperties);
        Assert.Single(scripts.AdditionalScripts);
        Assert.Equal(new[] { "echo additional" }, scripts.AdditionalScripts["additional_script"]);
    }

    [Fact]
    public void Deserialize_MinimumJson_Passes()
    {
        string json = "{}";

        PackageManifest.ScriptsType? scripts = JsonSerializer.Deserialize<PackageManifest.ScriptsType>(json);

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

        PackageManifest.ScriptsType? scripts = JsonSerializer.Deserialize<PackageManifest.ScriptsType>(json);

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

    [Fact]
    public void Deserialize_InvalidPropertyKey_Passes()
    {
        string json = """
            {
                "invalid-script": []
            }
            """;

        PackageManifest.ScriptsType? scripts = JsonSerializer.Deserialize<PackageManifest.ScriptsType>(json);

        Assert.NotNull(scripts);
        Assert.NotNull(scripts.AdditionalProperties);
        Assert.Single(scripts.AdditionalProperties);
        Assert.Equal([], scripts.AdditionalScripts);
    }

    [Fact]
    public void Deserialize_InvalidPropertyValueKind_Passes()
    {
        string json = """
            {
                "additional_script_1": null
            }
            """;

        PackageManifest.ScriptsType? scripts = JsonSerializer.Deserialize<PackageManifest.ScriptsType>(json);

        Assert.NotNull(scripts);
        Assert.NotNull(scripts.AdditionalProperties);
        Assert.Single(scripts.AdditionalProperties);
        Assert.Equal([], scripts.AdditionalScripts);
    }

    [Fact]
    public void Deserialize_InvalidPropertyItemValueKind_Passes()
    {
        string json = """
            {
                "additional_script": [
                    null
                ]
            }
            """;

        PackageManifest.ScriptsType? scripts = JsonSerializer.Deserialize<PackageManifest.ScriptsType>(json);

        Assert.NotNull(scripts);
        Assert.NotNull(scripts.AdditionalProperties);
        Assert.Single(scripts.AdditionalProperties);
        Assert.Equal([], scripts.AdditionalScripts);
    }
}

public class PackageManifest_VariantTypeTests
{
    [Fact]
    public void Deserialize_MinimumJson_Passes()
    {
        string json = "{}";

        PackageManifest.VariantType? variant = JsonSerializer.Deserialize<PackageManifest.VariantType>(json);

        Assert.NotNull(variant);
        Assert.Null(variant.Platform);
        Assert.Null(variant.Dependencies);
        Assert.Null(variant.Assets);
        Assert.Null(variant.Scripts);
    }

    [Fact]
    public void Deserialize_MaximumJson_Passes()
    {
        string json = """
            {
                "label": "",
                "platform": "",
                "dependencies": {},
                "assets": [],
                "scripts": {}
            }
            """;

        PackageManifest.VariantType? variant = JsonSerializer.Deserialize<PackageManifest.VariantType>(json);

        Assert.NotNull(variant);
        Assert.Equal("", variant.Label);
        Assert.Equal("", variant.Platform);
        Assert.Equal([], variant.Dependencies);
        Assert.Equal([], variant.Assets);
        Assert.NotNull(variant.Scripts);
    }
}

public class PackageManifestTests
{
    [Fact]
    public void FromBytes_MinimumJson_Passes()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "",
                "version": "1.0.0"
            }
            """u8.ToArray();

        var manifest = PackageManifest.FromBytes(bytes);

        Assert.Equal(3, manifest.FormatVersion);
        Assert.Equal("289f771f-2c9a-4d73-9f3f-8492495a924d", manifest.FormatUuid);
        Assert.Equal("", manifest.Tooth);
        Assert.Equal("1.0.0", manifest.Version);
    }

    [Fact]
    public void FromBytes_MaximumJson_Passes()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "",
                "version": "1.0.0",
                "info": {},
                "variants": []
            }
            """u8.ToArray();

        var manifest = PackageManifest.FromBytes(bytes);

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
        Assert.Throws<ArgumentException>("bytes", () => PackageManifest.FromBytes("null"u8.ToArray()));
    }

    [Fact]
    public void FromBytes_InvalidFormatVersion_ThrowsArgumentException()
    {
        byte[] bytes = """
            {
                "format_version": 0,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "",
                "version": "1.0.0"
            }
            """u8.ToArray();

        Assert.Throws<ArgumentException>("value", () => PackageManifest.FromBytes(bytes));
    }

    [Fact]
    public void FromBytes_InvalidFormatUuid_ThrowsArgumentException()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "",
                "tooth": "",
                "version": "1.0.0"
            }
            """u8.ToArray();

        Assert.Throws<ArgumentException>("value", () => PackageManifest.FromBytes(bytes));
    }

    [Fact]
    public void FromBytes_InvalidVersion_ThrowsArgumentException()
    {
        byte[] bytes = """
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "",
                "version": ""
            }
            """u8.ToArray();

        Assert.Throws<ArgumentException>("value", () => PackageManifest.FromBytes(bytes));
    }

    [Fact]
    public void ToBytes_MinimumJson_Passes()
    {
        var manifest = new PackageManifest
        {
            FormatVersion = 3,
            FormatUuid = "289f771f-2c9a-4d73-9f3f-8492495a924d",
            Tooth = "",
            Version = "1.0.0"
        };

        byte[] bytes = manifest.ToBytes();

        string json = Encoding.UTF8.GetString(bytes);

        Assert.Equal("""
            {
                "format_version": 3,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "",
                "version": "1.0.0"
            }
            """, json);
    }
}
