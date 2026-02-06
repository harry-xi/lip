using Lip.Core.Migration.PackageManifests;
using System.Text.Json;

namespace Lip.Core.Tests.Migration.PackageManifests;

public class MigrationTests
{
    [Fact]
    public void Migrate_V1_To_V3_Success()
    {
        string v1Json = """
        {
            "format_version": 1,
            "tooth": "github.com/user/example",
            "version": "1.0.0",
            "information": {
                "name": "Test Package",
                "description": "A test package"
            },
            "placement": [
                {
                    "source": "bin/App.exe",
                    "destination": "App.exe"
                }
            ],
            "commands": [
                {
                    "type": "install",
                    "commands": ["echo installed"],
                    "GOOS": "windows",
                    "GOARCH": "amd64"
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(v1Json);
        var manifest = MigrationV1ToV3.Migrate(doc);

        Assert.Equal("github.com/user/example", manifest.Path);
        Assert.Equal("1.0.0", manifest.Version.ToString());
        Assert.Equal("Test Package", manifest.Info.Name);

        // V1 commands are mapped to variants
        var variant = manifest.Variants.FirstOrDefault();
        Assert.NotNull(variant);
        Assert.Contains("echo installed", variant.Scripts.PostInstall);

        // Placement mapped to Assets
        Assert.Single(variant.Assets);
        Assert.Equal("bin/App.exe", variant.Assets[0].Placements[0].Src);
    }

    [Fact]
    public void Migrate_V2_To_V3_Success()
    {
        string v2Json = """
        {
            "format_version": 2,
            "tooth": "github.com/user/example",
            "version": "2.0.0",
            "info": {
                "name": "Test Package V2",
                "description": "V2 Description",
                "author": "Me",
                "tags": ["tool"]
            },
            "platforms": [
                {
                    "goos": "windows",
                    "goarch": "amd64",
                    "asset_url": "https://example.com/release.zip",
                    "files": {
                        "place": [
                            { "src": "bin/app.exe", "dest": "app.exe" }
                        ]
                    }
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(v2Json);
        var manifest = MigrationV1ToV3.Migrate(doc);

        Assert.Equal("github.com/user/example", manifest.Path);
        Assert.Equal("2.0.0", manifest.Version.ToString());

        var variant = manifest.Variants.Single();
        Assert.Equal("win-x64", variant.Platform);
        Assert.Single(variant.Assets);
        Assert.Equal("https://example.com/release.zip", variant.Assets[0].Urls[0].ToString());
    }

    [Fact]
    public void Migrate_V2_With_Variable_Substitution()
    {
        string v2Json = """
        {
            "format_version": 2,
            "tooth": "github.com/user/example",
            "version": "1.0.0",
            "info": { "name": "VarTest", "description": "", "author": "", "tags": [] },
            "platforms": [
                {
                    "goos": "linux",
                    "goarch": "amd64",
                    "asset_url": "https://example.com/$(version)/file.tar.gz"
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(v2Json);
        var manifest = MigrationV1ToV3.Migrate(doc);

        var variant = manifest.Variants.Single();
        // Checked against updated syntax
        Assert.Contains("{{version}}", variant.Assets[0].Urls[0].ToString());
    }
}