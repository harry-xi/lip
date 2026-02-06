using Lip.Core.Entities;
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
            _ => throw new NotSupportedException($"Unsupported format version: {formatVersion}")
        };
    }

    private static PackageManifestV2 MigrateV1ToV2(PackageManifestV1 manifestV1)
    {
        var data = manifestV1.Information?.Data;
        var info = new PackageManifestV2Info
        {
            Name = data != null && data.TryGetValue("name", out var n)
                ? n.GetString() ?? ""
                : "",
            Description = data != null && data.TryGetValue("description", out var d)
                ? d.GetString() ?? ""
                : "",
            Author = data != null && data.TryGetValue("author", out var a)
                ? a.GetString() ?? ""
                : "",
            Tags = data != null && data.TryGetValue("tags", out var t) && t.ValueKind == JsonValueKind.Array
                ? [.. t.EnumerateArray().Select(e => e.GetString() ?? "")]
                : []
        };

        static PackageManifestV2Commands? ConvertCommands(List<PackageManifestV1Command>? commands)
        {
            if (commands == null)
                return null;

            var result = new PackageManifestV2Commands();
            foreach (var cmd in commands)
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
            return (result.PostInstall == null && result.PostUninstall == null) ? null : result;
        }

        return new PackageManifestV2
        {
            FormatVersion = 2,
            Tooth = manifestV1.Tooth,
            Version = manifestV1.Version,
            Info = info,
            AssetUrl = null,
            Commands = ConvertCommands(manifestV1.Commands),
            Dependencies = manifestV1.Dependencies?.ToDictionary(
                kvp => kvp.Key,
                kvp => string.Join(" || ", kvp.Value.Select(group => string.Join(" && ", group)))
            ),
            Prerequisites = null,
            Files = manifestV1.Placement != null
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
        Func<string?, string?> substitute = input =>
            input == null ? null : System.Text.RegularExpressions.Regex.Replace(input, @"\$\(([^)]+?)\)", "{{$1}}");

        var manifest = new PackageManifest
        {
            Path = manifestV2.Tooth,
            Version = Semver.SemVersion.Parse(manifestV2.Version, Semver.SemVersionStyles.Any),
            Info = new PackageManifestInfo
            {
                Name = manifestV2.Info.Name,
                Description = manifestV2.Info.Description,
                Tags = manifestV2.Info.Tags,
                AvatarUrl = manifestV2.Info.AvatarUrl is null ? null : new Flurl.Url(substitute(manifestV2.Info.AvatarUrl))
            },
            Variants =
            [
                .. manifestV2.Platforms?
                       .Select(p => new PackageManifestVariant
                       {
                           Platform = ConvertGOARCHAndGOOSToPlatform(p.GOARCH, p.GOOS),
                           Dependencies = ConvertDependencies(p.Dependencies ?? manifestV2.Dependencies),
                           Assets = p.AssetUrl is not null
                               ?
                               [
                                   new PackageManifestAsset
                                   {
                                       Type = ConvertAssetType(p.AssetUrl),
                                       Urls = [new Flurl.Url(substitute(p.AssetUrl)!)],
                                       Placements = (p.Files?.Place?.Select(pl => ConvertPlaceToPlacement(pl, substitute)).ToList())
                                            ?? (manifestV2.Files?.Place?.Select(pl => ConvertPlaceToPlacement(pl, substitute)).ToList()) ?? []
                                   }
                               ]
                               : manifestV2.AssetUrl is not null
                                   ?
                                   [
                                       new PackageManifestAsset
                                       {
                                           Type = ConvertAssetType(manifestV2.AssetUrl),
                                           Urls = [new Flurl.Url(substitute(manifestV2.AssetUrl)!)],
                                           Placements = manifestV2.Files?.Place?.Select(pl => ConvertPlaceToPlacement(pl, substitute)).ToList() ?? []
                                       }
                                   ]
                                   : [],
                           PreserveFiles = p.Files?.Preserve?.Select(s => substitute(s)!).ToList()
                                           ?? manifestV2.Files?.Preserve?.Select(s => substitute(s)!).ToList() ?? [],
                           RemoveFiles = p.Files?.Remove?.Select(s => substitute(s)!).ToList()
                                         ?? manifestV2.Files?.Remove?.Select(s => substitute(s)!).ToList() ?? [],
                           Scripts = p.Commands is not null
                               ? ConvertCommandsToScripts(p.Commands, substitute)
                               : manifestV2.Commands is not null
                                   ? ConvertCommandsToScripts(manifestV2.Commands, substitute)
                                   : new()
                       }) ??
                   [
                       new PackageManifestVariant
                       {
                           Dependencies = ConvertDependencies(manifestV2.Dependencies),
                           Assets = manifestV2.AssetUrl is not null
                               ?
                               [
                                   new PackageManifestAsset
                                   {
                                       Type = ConvertAssetType(manifestV2.AssetUrl),
                                       Urls = [new Flurl.Url(substitute(manifestV2.AssetUrl)!)],
                                       Placements = manifestV2.Files?.Place?.Select(pl => ConvertPlaceToPlacement(pl, substitute)).ToList() ?? []
                                   }
                               ]
                               :
                               [
                                   new PackageManifestAsset
                                   {
                                       Type = PackageManifestAsset.AssetType.Self,
                                       Placements = manifestV2.Files?.Place?.Select(pl => ConvertPlaceToPlacement(pl, substitute)).ToList() ?? []
                                   }
                               ],
                           PreserveFiles = manifestV2.Files?.Preserve?.Select(s => substitute(s)!).ToList() ?? [],
                           RemoveFiles = manifestV2.Files?.Remove?.Select(s => substitute(s)!).ToList() ?? [],
                           Scripts = manifestV2.Commands is not null
                               ? ConvertCommandsToScripts(manifestV2.Commands, substitute)
                               : new()
                       }
                   ]
            ]
        };

        if (manifest.Variants.Any(variant =>
                !string.IsNullOrEmpty(variant.Platform) &&
                manifest.Variants.Any(s => s.Platform != null && s.Platform.Contains('*'))))
        {
            manifest.Variants.Insert(0, new PackageManifestVariant { Platform = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier });
        }

        return manifest;
    }

    private static PackageManifestScripts ConvertCommandsToScripts(PackageManifestV2Commands commands, Func<string?, string?> substitute)
    {
        return new PackageManifestScripts
        {
            PreInstall = commands.PreInstall?.Select(s => substitute(s)!).ToList() ?? [],
            PostInstall = commands.PostInstall?.Select(s => substitute(s)!).ToList() ?? [],
            PreUninstall = commands.PreUninstall?.Select(s => substitute(s)!).ToList() ?? [],
            PostUninstall = commands.PostUninstall?.Select(s => substitute(s)!).ToList() ?? []
        };
    }

    private static string ConvertGOARCHAndGOOSToPlatform(string? goarch, string goos)
    {
        string prefix = goos switch
        {
            "windows" => "win",
            "darwin" => "osx",
            "linux" => "linux",
            "ios" => "ios",
            "android" => "android",
            _ => throw new PlatformNotSupportedException($"Unsupported GOOS: {goos}")
        };

        string suffix = goarch switch
        {
            "amd64" => "x64",
            "386" => "x86",
            "arm64" => "arm64",
            "arm" => "arm",
            "loong64" => "loongarch64",
            null => "*",
            _ => throw new PlatformNotSupportedException($"Unsupported GOARCH: {goarch}")
        };

        return $"{prefix}-{suffix}";
    }

    private static PackageManifestAssetPlacement ConvertPlaceToPlacement(PackageManifestV2Place places, Func<string?, string?> substitute)
    {
        return new PackageManifestAssetPlacement
        {
            Src = substitute(places.Src)?.TrimEnd('*') ?? "",
            Dst = substitute(places.Dest) ?? "",
            Type = places.Src.EndsWith('*')
                ? PackageManifestAssetPlacement.PlacementType.Dir
                : PackageManifestAssetPlacement.PlacementType.File
        };
    }

    private static PackageManifestAsset.AssetType ConvertAssetType(string url)
    {
        return url.Split('.').Last() switch
        {
            "tar" => PackageManifestAsset.AssetType.Tar,
            "tgz" => PackageManifestAsset.AssetType.Tgz,
            "gz" => PackageManifestAsset.AssetType.Tgz,
            "zip" => PackageManifestAsset.AssetType.Zip,
            _ => PackageManifestAsset.AssetType.Uncompressed
        };
    }

    private static Dictionary<PackageId, Semver.SemVersionRange> ConvertDependencies(Dictionary<string, string>? dependencies)
    {
        if (dependencies == null) return [];

        var result = new Dictionary<PackageId, Semver.SemVersionRange>();
        foreach (var kvp in dependencies)
        {
            try
            {
                result[PackageId.Parse(kvp.Key)] = Semver.SemVersionRange.Parse(kvp.Value);
            }
            catch
            {
                // Ignore invalid dependencies
            }
        }
        return result;
    }
}