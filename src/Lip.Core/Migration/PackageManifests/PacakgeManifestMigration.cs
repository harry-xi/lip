using DotNet.Globbing;
using Lip.Core.Entities;
using Semver;
using System.Diagnostics;
using System.Text.Json;

namespace Lip.Core.Migration.PackageManifests;

public static class PackageManifestMigration
{
    public static PackageManifest Migrate(JsonDocument jsonDocument)
    {
        int formatVersion = jsonDocument.RootElement.GetProperty("format_version").GetInt32();

        return formatVersion switch
        {
            1 => MigrateV2ToV3(MigrateV1ToV2(JsonSerializer.Deserialize<PackageManifestV1>(jsonDocument)!)),
            2 => MigrateV2ToV3(JsonSerializer.Deserialize<PackageManifestV2>(jsonDocument)!),
            3 => JsonSerializer.Deserialize<PackageManifest>(jsonDocument)!,
            _ => throw new UnreachableException(),
        };
    }

    private static PackageManifestV2 MigrateV1ToV2(PackageManifestV1 manifestV1)
    {
        Dictionary<string, JsonElement>? data = manifestV1.Information?.Data;
        PackageManifestV2Info info = new()
        {
            Name = data is not null && data.TryGetValue("name", out JsonElement n)
                ? n.GetString() ?? ""
                : "",
            Description = data is not null && data.TryGetValue("description", out JsonElement d)
                ? d.GetString() ?? ""
                : "",
            Author = data is not null && data.TryGetValue("author", out JsonElement a)
                ? a.GetString() ?? ""
                : "",
            Tags = data is not null && data.TryGetValue("tags", out JsonElement t) && t.ValueKind == JsonValueKind.Array
                ? [.. t.EnumerateArray().Select(e => e.GetString() ?? "")]
                : []
        };

        PackageManifestV2Commands? commands = null;
        if (manifestV1.Commands is not null)
        {
            PackageManifestV2Commands result = new();
            foreach (PackageManifestV1Command cmd in manifestV1.Commands)
            {
                if (cmd.Type.Equals("install", StringComparison.OrdinalIgnoreCase))
                {
                    result.PostInstall ??= [];
                    result.PostInstall.AddRange(cmd.Commands);
                }
                else if (cmd.Type.Equals("uninstall", StringComparison.OrdinalIgnoreCase))
                {
                    result.PostUninstall ??= [];
                    result.PostUninstall.AddRange(cmd.Commands);
                }
            }
            if (result.PostInstall is not null || result.PostUninstall is not null)
            {
                commands = result;
            }
        }

        return new PackageManifestV2
        {
            FormatVersion = 2,
            Tooth = manifestV1.Tooth,
            Version = manifestV1.Version,
            Info = info,
            AssetUrl = null,
            Commands = commands,
            Dependencies = manifestV1.Dependencies?.ToDictionary(
                kvp => kvp.Key,
                kvp => string.Join(" || ", kvp.Value.Select(group => string.Join(" && ", group)))
            ),
            Prerequisites = null,
            Files = manifestV1.Placement is not null
                ? new PackageManifestV2Files
                {
                    Place = [.. manifestV1.Placement.Select(p => new PackageManifestV2Place { Src = p.Source, Dest = p.Destination.TrimEnd('*') })]
                }
                : null,
            Platforms = null
        };
    }

    private static PackageManifest MigrateV2ToV3(PackageManifestV2 manifestV2)
    {
        PackageManifest manifest = new()
        {
            Path = manifestV2.Tooth,
            Version = Semver.SemVersion.Parse(manifestV2.Version, Semver.SemVersionStyles.Any),
            Info = new PackageManifestInfo
            {
                Name = manifestV2.Info.Name,
                Description = manifestV2.Info.Description,
                Tags = manifestV2.Info.Tags,
                AvatarUrl = manifestV2.Info.AvatarUrl is null
                    ? null
                    : new Flurl.Url(System.Text.RegularExpressions.Regex.Replace(manifestV2.Info.AvatarUrl, @"\$\(([^)]+?)\)", "{{$1}}"))
            },
            Variants = []
        };

        if (manifestV2.Platforms is not null)
        {
            foreach (PackageManifestV2Platform p in manifestV2.Platforms)
            {
                string prefix = p.GOOS switch
                {
                    "windows" => "win",
                    "darwin" => "osx",
                    "linux" => "linux",
                    "ios" => "ios",
                    "android" => "android",
                    _ => throw new PlatformNotSupportedException($"Unsupported GOOS: {p.GOOS}")
                };

                string suffix = p.GOARCH switch
                {
                    "amd64" => "x64",
                    "386" => "x86",
                    "arm64" => "arm64",
                    "arm" => "arm",
                    "loong64" => "loongarch64",
                    null => "*",
                    _ => throw new PlatformNotSupportedException($"Unsupported GOARCH: {p.GOARCH}")
                };

                Dictionary<PackageId, SemVersionRange> dependencies = [];
                if (p.Dependencies is not null || manifestV2.Dependencies is not null)
                {
                    foreach (KeyValuePair<string, string> kvp in p.Dependencies ?? manifestV2.Dependencies!)
                    {
                        try
                        {
                            dependencies[PackageId.Parse(kvp.Key)] = Semver.SemVersionRange.ParseNpm(kvp.Value);
                        }
                        catch
                        {
                            // Ignore invalid dependencies
                        }
                    }
                }

                List<PackageManifestAsset> assets = [];
                string? assetUrl = p.AssetUrl ?? manifestV2.AssetUrl;
                if (assetUrl is not null)
                {
                    string url = System.Text.RegularExpressions.Regex.Replace(assetUrl, @"\$\(([^)]+?)\)", "{{$1}}");
                    PackageManifestAsset.AssetType type = url.Split('.').Last() switch
                    {
                        "tar" => PackageManifestAsset.AssetType.Tar,
                        "tgz" => PackageManifestAsset.AssetType.Tgz,
                        "gz" => PackageManifestAsset.AssetType.Tgz,
                        "zip" => PackageManifestAsset.AssetType.Zip,
                        _ => PackageManifestAsset.AssetType.Uncompressed
                    };

                    List<PackageManifestAssetPlacement> placements = [];
                    List<PackageManifestV2Place>? places = p.Files?.Place ?? manifestV2.Files?.Place;
                    if (places is not null)
                    {
                        foreach (PackageManifestV2Place pl in places)
                        {
                            string src = System.Text.RegularExpressions.Regex.Replace(pl.Src, @"\$\(([^)]+?)\)", "{{$1}}")?.TrimEnd('*') ?? "";
                            string dst = System.Text.RegularExpressions.Regex.Replace(pl.Dest, @"\$\(([^)]+?)\)", "{{$1}}") ?? "";
                            placements.Add(new PackageManifestAssetPlacement
                            {
                                Src = src,
                                Dst = dst,
                                Type = pl.Src.EndsWith('*')
                                    ? PackageManifestAssetPlacement.PlacementType.Directory
                                    : PackageManifestAssetPlacement.PlacementType.File
                            });
                        }
                    }

                    assets.Add(new PackageManifestAsset
                    {
                        Type = type,
                        Urls = [new Flurl.Url(url)],
                        Placements = placements
                    });
                }

                PackageManifestScripts scripts = new();
                PackageManifestV2Commands? cmds = p.Commands ?? manifestV2.Commands;
                if (cmds is not null)
                {
                    scripts = new PackageManifestScripts
                    {
                        PreInstall = cmds.PreInstall?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).ToList() ?? [],
                        PostInstall = cmds.PostInstall?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).ToList() ?? [],
                        PreUninstall = cmds.PreUninstall?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).ToList() ?? [],
                        PostUninstall = cmds.PostUninstall?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).ToList() ?? []
                    };
                }

                manifest.Variants.Add(new PackageManifestVariant
                {
                    Platform = $"{prefix}-{suffix}",
                    Dependencies = dependencies,
                    Assets = assets,
                    PreserveFiles = (p.Files?.Preserve ?? manifestV2.Files?.Preserve)?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).Select(s => Glob.Parse(s)).ToList() ?? [],
                    RemoveFiles = (p.Files?.Remove ?? manifestV2.Files?.Remove)?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).Select(s => Glob.Parse(s)).ToList() ?? [],
                    Scripts = scripts
                });
            }
        }
        else
        {
            Dictionary<PackageId, SemVersionRange> dependencies = [];
            if (manifestV2.Dependencies is not null)
            {
                foreach (KeyValuePair<string, string> kvp in manifestV2.Dependencies)
                {
                    try
                    {
                        dependencies[PackageId.Parse(kvp.Key)] = Semver.SemVersionRange.Parse(kvp.Value);
                    }
                    catch
                    {
                        // Ignore invalid dependencies
                    }
                }
            }

            List<PackageManifestAsset> assets = [];
            if (manifestV2.AssetUrl is not null)
            {
                string url = System.Text.RegularExpressions.Regex.Replace(manifestV2.AssetUrl, @"\$\(([^)]+?)\)", "{{$1}}");
                PackageManifestAsset.AssetType type = url.Split('.').Last() switch
                {
                    "tar" => PackageManifestAsset.AssetType.Tar,
                    "tgz" => PackageManifestAsset.AssetType.Tgz,
                    "gz" => PackageManifestAsset.AssetType.Tgz,
                    "zip" => PackageManifestAsset.AssetType.Zip,
                    _ => PackageManifestAsset.AssetType.Uncompressed
                };

                List<PackageManifestAssetPlacement> placements = [];
                if (manifestV2.Files?.Place is not null)
                {
                    foreach (PackageManifestV2Place pl in manifestV2.Files.Place)
                    {
                        string src = System.Text.RegularExpressions.Regex.Replace(pl.Src, @"\$\(([^)]+?)\)", "{{$1}}")?.TrimEnd('*') ?? "";
                        string dst = System.Text.RegularExpressions.Regex.Replace(pl.Dest, @"\$\(([^)]+?)\)", "{{$1}}") ?? "";
                        placements.Add(new PackageManifestAssetPlacement
                        {
                            Src = src,
                            Dst = dst,
                            Type = pl.Src.EndsWith('*')
                                ? PackageManifestAssetPlacement.PlacementType.Directory
                                : PackageManifestAssetPlacement.PlacementType.File
                        });
                    }
                }

                assets.Add(new PackageManifestAsset
                {
                    Type = type,
                    Urls = [new Flurl.Url(url)],
                    Placements = placements
                });
            }
            else
            {
                List<PackageManifestAssetPlacement> placements = [];
                if (manifestV2.Files?.Place is not null)
                {
                    foreach (PackageManifestV2Place pl in manifestV2.Files.Place)
                    {
                        string src = System.Text.RegularExpressions.Regex.Replace(pl.Src, @"\$\(([^)]+?)\)", "{{$1}}")?.TrimEnd('*') ?? "";
                        string dst = System.Text.RegularExpressions.Regex.Replace(pl.Dest, @"\$\(([^)]+?)\)", "{{$1}}") ?? "";
                        placements.Add(new PackageManifestAssetPlacement
                        {
                            Src = src,
                            Dst = dst,
                            Type = pl.Src.EndsWith('*')
                                ? PackageManifestAssetPlacement.PlacementType.Directory
                                : PackageManifestAssetPlacement.PlacementType.File
                        });
                    }

                    assets.Add(new PackageManifestAsset
                    {
                        Type = PackageManifestAsset.AssetType.Self,
                        Placements = placements
                    });
                }
            }

            PackageManifestScripts scripts = new();
            if (manifestV2.Commands is not null)
            {
                scripts = new PackageManifestScripts
                {
                    PreInstall = manifestV2.Commands.PreInstall?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).ToList() ?? [],
                    PostInstall = manifestV2.Commands.PostInstall?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).ToList() ?? [],
                    PreUninstall = manifestV2.Commands.PreUninstall?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).ToList() ?? [],
                    PostUninstall = manifestV2.Commands.PostUninstall?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).ToList() ?? []
                };
            }

            manifest.Variants.Add(new PackageManifestVariant
            {
                Dependencies = dependencies,
                Assets = assets,
                PreserveFiles = manifestV2.Files?.Preserve?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).Select(s => Glob.Parse(s)).ToList() ?? [],
                RemoveFiles = manifestV2.Files?.Remove?.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\$\(([^)]+?)\)", "{{$1}}")).Select(s => Glob.Parse(s)).ToList() ?? [],
                Scripts = scripts
            });
        }

        if (manifest.Variants.Any(variant =>
                !string.IsNullOrEmpty(variant.Platform) &&
                manifest.Variants.Any(s => s.Platform is not null && s.Platform.Contains('*'))))
        {
            manifest.Variants.Insert(0, new PackageManifestVariant { Platform = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier });
        }

        return manifest;
    }
}