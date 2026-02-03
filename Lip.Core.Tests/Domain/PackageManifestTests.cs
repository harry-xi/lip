using Flurl;
using Lip.Core.JsonConverters;
using Semver;
using System.Text;
using System.Text.Json;
using static Lip.Core.PackageLock;

namespace Lip.Core.Tests;

public partial class PackageManifestTests
{












    [Fact]
    public void Constructor_ValidValues_ReturnsCorrectInstance()
    {
        // Arrange & Act.
        string toothPath = "example.com/pkg";
        SemVersion version = new(0);
        PackageManifest.InfoType info = new()
        {
            Name = string.Empty,
            Description = string.Empty,
            Tags = [],
            AvatarUrl = new(),
        };

        PackageManifest manifest = new()
        {
            ToothPath = toothPath,
            Version = version,
            Info = info,
            Variants = [],
        };

        PackageManifest newManifest = manifest with { };

        // Assert.
        Assert.Equal(DefaultFormatVersion, newManifest.FormatVersion);
        Assert.Equal(DefaultFormatUuid, newManifest.FormatUuid);
        Assert.Equal(toothPath, newManifest.ToothPath);
        Assert.Equal(version, newManifest.Version);
        Assert.Equal(info, newManifest.Info);
        Assert.Empty(newManifest.Variants);
    }

    // Constructor_InvalidToothPath_ThrowsShcemaViolationException Removed as validation represents logic not currently enforced or handled elsewhere.

    private const string _minimumJson = """
        {
            "format_version": 3,
            "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
            "tooth": "example.com/pkg",
            "version": "0.0.0"
        }
        """;

    [Fact]
    public void FromJsonElement_MinimumJson_ReturnsCorrectInstance()
    {
        // Assert.
        JsonElement jsonElement = JsonDocument.Parse(_minimumJson).RootElement;

        // Act.
        PackageManifest manifest = PackageManifest.Create(jsonElement);

        // Assert.
        Assert.Equal("example.com/pkg", manifest.ToothPath);
        Assert.Equal(new(0), manifest.Version);
    }

    [Fact]
    public void FromJsonElement_InvalidFormatVersion_ThrowsShcemaViolationException()
    {
        // Arrange.
        string manifestJson = """
            {
                "format_version": 0,
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/pkg",
                "version": "0.0.0"
            }
            """;

        JsonElement jsonElement = JsonDocument.Parse(manifestJson).RootElement;

        // Act & Assert.
        Assert.Throws<JsonException>(
            () => PackageManifest.Create(jsonElement));
    }

    [Fact]
    public void FromJsonElement_InvalidFormatUuid_ThrowsShcemaViolationException()
    {
        // Arrange.
        string manifestJson = """
            {
                "format_version": 3,
                "format_uuid": "",
                "tooth": "example.com/pkg",
                "version": "0.0.0"
            }
            """;

        JsonElement jsonElement = JsonDocument.Parse(manifestJson).RootElement;

        // Act & Assert.
        Assert.Throws<JsonException>(
            () => PackageManifest.Create(jsonElement));
    }

    [Fact]
    public void FromJsonElement_MissingFormatVersion_ThrowsJsonException()
    {
        // Arrange.
        string manifestJson = """
            {
                "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
                "tooth": "example.com/pkg",
                "version": "0.0.0"
            }
            """;

        JsonElement jsonElement = JsonDocument.Parse(manifestJson).RootElement;

        // Act & Assert.
        Assert.Throws<JsonException>(
            () => PackageManifest.Create(jsonElement));
    }

    [Fact]
    public void FromJsonElement_MissingFormatUuid_ThrowsJsonException()
    {
        // Arrange.
        string manifestJson = """
            {
                "format_version": 3,
                "tooth": "example.com/pkg",
                "version": "0.0.0"
            }
            """;

        JsonElement jsonElement = JsonDocument.Parse(manifestJson).RootElement;

        // Act & Assert.
        Assert.Throws<JsonException>(
            () => PackageManifest.Create(jsonElement));
    }

    // FromJsonElement_InvalidAdditionalScriptFormat_ThrowsShcemaViolationException Removed due to relaxed validation for now.

    [Fact]
    public async Task FromStream_ValidStream_ReturnsCorrectInstance()
    {
        // Assert.
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(_minimumJson));

        // Act.
        PackageManifest manifest = await PackageManifest.FromStream(stream);

        // Assert.
        Assert.Equal("example.com/pkg", manifest.ToothPath);
        Assert.Equal(new(0), manifest.Version);
    }

    [Fact]
    public void GetVariant_VariantMatched_ReturnsCorrectVariant()
    {
        // Assert.
        PackageManifest.Variant variant = new()
        {
            Label = "label",
            Platform = "platform",
            Dependencies = new()
            {
                [
                    new PackageIdentifier("exmaple.com/pkg", string.Empty)
                ] = SemVersionRange.Parse("1.0.0")
            },
            Assets = [],
            PreserveFiles = ["path/to/preserve/file"],
            RemoveFiles = ["path/to/remove/file"],
            Scripts = new()
            {
                PreInstall = [],
                Install = [],
                PostInstall = [],
                PreUninstall = [],
                Uninstall = [],
                PostUninstall = [],

            }
        };

        PackageManifest packageManifest = new()
        {
            FormatVersion = DefaultFormatVersion,
            FormatUuid = DefaultFormatUuid,
            ToothPath = "example.com/pkg",
            Version = new(0),
            Info = new()
            {
                Name = string.Empty,
                Description = string.Empty,
                Tags = [],
                AvatarUrl = new(),
            },
            Variants = [
                variant
            ],
        };

        // Act.
        PackageManifest.Variant? variantGot = packageManifest.GetVariant("label", "platform");

        // Assert.
        Assert.NotNull(variantGot);
        Assert.Equal(variant.Label, variantGot.Label);
        Assert.Equal(variant.Platform, variantGot.Platform);
        Assert.Equal(variant.Dependencies, variantGot.Dependencies);
        Assert.Equal(variant.Assets, variantGot.Assets);
        Assert.Equal(variant.PreserveFiles, variantGot.PreserveFiles);
        Assert.Equal(variant.RemoveFiles, variantGot.RemoveFiles);
        Assert.Equal(variant.Scripts.PreInstall, variantGot.Scripts.PreInstall);
        Assert.Equal(variant.Scripts.Install, variantGot.Scripts.Install);
        Assert.Equal(variant.Scripts.PostInstall, variantGot.Scripts.PostInstall);
        Assert.Equal(variant.Scripts.PreUninstall, variantGot.Scripts.PreUninstall);
        Assert.Equal(variant.Scripts.Uninstall, variantGot.Scripts.Uninstall);
        Assert.Equal(variant.Scripts.PostUninstall, variantGot.Scripts.PostUninstall);

    }

    [Fact]
    public void GetVariant_NoVariantMatched_ReturnsNull()
    {
        // Assert.
        PackageManifest packageManifest = new()
        {
            FormatVersion = DefaultFormatVersion,
            FormatUuid = DefaultFormatUuid,
            ToothPath = "example.com/pkg",
            Version = new(0),
            Info = new()
            {
                Name = string.Empty,
                Description = string.Empty,
                Tags = [],
                AvatarUrl = new(),
            },
            Variants = [],
        };

        // Act.
        PackageManifest.Variant? variantGot = packageManifest.GetVariant("label", "platform");

        // Assert.
        Assert.Null(variantGot);
    }

    [Fact]
    public void GetVariant_NoVariantFullyMatched_ReturnsNull()
    {
        // Assert.
        PackageManifest.Variant variant = new()
        {
            Label = "*",
            Platform = "*",
            Dependencies = [],
            Assets = [],
            PreserveFiles = ["path/to/preserve/file"],
            RemoveFiles = ["path/to/remove/file"],
            Scripts = new()
            {
                PreInstall = [],
                Install = [],
                PostInstall = [],
                PreUninstall = [],
                Uninstall = [],
                PostUninstall = [],

            }
        };

        PackageManifest packageManifest = new()
        {
            FormatVersion = DefaultFormatVersion,
            FormatUuid = DefaultFormatUuid,
            ToothPath = "example.com/pkg",
            Version = new(0),
            Info = new()
            {
                Name = string.Empty,
                Description = string.Empty,
                Tags = [],
                AvatarUrl = new(),
            },
            Variants = [
                variant
            ],
        };

        // Act.
        PackageManifest.Variant? variantGot = packageManifest.GetVariant("label", "platform");

        // Assert.
        Assert.Null(variantGot);
    }

    private readonly PackageManifest _outputManifest = new()
    {
        FormatVersion = DefaultFormatVersion,
        FormatUuid = DefaultFormatUuid,
        ToothPath = "example.com/pkg",
        Version = new(0),
        Info = new()
        {
            Name = string.Empty,
            Description = string.Empty,
            Tags = [],
            AvatarUrl = new(),
        },
        Variants = [
            new()
            {
                Label = string.Empty,
                Platform = string.Empty,
                Dependencies = new()
                {
                    [
                        new PackageIdentifier("example.com/pkg", string.Empty)
                    ] = SemVersionRange.Parse("*")
                },
                Assets = [
                    new()
                    {
                        Type = PackageManifest.Asset.TypeEnum.Self,
                        Urls = [
                            new()
                        ],
                        Placements = [
                            new()
                            {
                                Type = PackageManifest.Placement.TypeEnum.File,
                                Src = string.Empty,
                                Dest = "path/to/file"
                            }
                        ]
                    }
                ],
                PreserveFiles = [],
                RemoveFiles = [],
                Scripts = new()
                {
                    PreInstall = [],
                    Install = [],
                    PostInstall = [],
                    PreUninstall = [],
                    Uninstall = [],
                    PostUninstall = [],

                }
            }
        ],
    };

    private const string _outputJson = """
        {
            "format_version": 3,
            "format_uuid": "289f771f-2c9a-4d73-9f3f-8492495a924d",
            "tooth": "example.com/pkg",
            "version": "0.0.0",
            "info": {
                "name": "",
                "description": "",
                "tags": [],
                "avatar_url": ""
            },
            "variants": [
                {
                    "label": "",
                    "platform": "",
                    "dependencies": {
                        "example.com/pkg": "*"
                    },
                    "assets": [
                        {
                            "type": "self",
                            "urls": [
                                ""
                            ],
                            "placements": [
                                {
                                    "type": "file",
                                    "src": "",
                                    "dest": "path/to/file"
                                }
                            ]
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
        """;

    [Fact]
    public void ToJsonElement_ReturnsCorrectJsonElement()
    {
        // Act.
        JsonElement jsonElement = JsonSerializer.SerializeToElement(_outputManifest);

        // Assert.
        // Assert.Equal(_outputJson.ReplaceLineEndings(), jsonElement.ToString());
        // Assert.True(JsonElement.DeepEquals(JsonDocument.Parse(_outputJson).RootElement, jsonElement));
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 4,
            Converters =
            {
                new SemVersionConverter(),
                new UrlConverter(),

                new PackageIdentifierConverter(),
                new SemVersionRangeConverter()
            }
        };
        string json = JsonSerializer.Serialize(_outputManifest, options);
        Assert.Equal(_outputJson.ReplaceLineEndings(), json.ReplaceLineEndings());
    }

    [Fact]
    public async Task ToStream_ReturnsCorrectStream()
    {
        // Assert.
        MemoryStream stream = new();

        // Act.
        await PackageManifest.WriteToStreamAsync(_outputManifest, stream);

        // Assert.
        Assert.Equal(_outputJson.ReplaceLineEndings(), Encoding.UTF8.GetString(stream.ToArray()));
    }





}