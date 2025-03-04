using System.Runtime.InteropServices;
using System.Text.Json;

namespace Lip.Migration;

public static class MigratorFromV2
{
    public static bool IsMigratable(JsonElement json)
    {
        return json.TryGetProperty("format_version", out var version)
               && version.TryGetInt32(out var v)
               && v == 2;
    }

    public static JsonElement Migrate(JsonElement json)
    {
        var manifestV2 = json.Deserialize<ManifestV2>()
            ?? throw new JsonException("Failed to deserialize the obsolete manifest.");

        // Find a platform configuration matching the current runtime identifier.
        var matchingPlatform = manifestV2.Platforms?
            .FirstOrDefault(platform => GetRIDFromGo(platform.GOARCH, platform.GOOS) == RuntimeInformation.RuntimeIdentifier);

        var manifest = new Manifest
        {
            FormatVersion = 3,
            FormatUuid = "289f771f-2c9a-4d73-9f3f-8492495a924d",
            Tooth = manifestV2.Tooth,
            Version = manifestV2.Version,
            Info = new Manifest.InfoType
            {
                Name = manifestV2.Info.Name,
                Description = manifestV2.Info.Description,
                Tags = manifestV2.Info.Tags,
                AvatarUrl = manifestV2.Info.AvatarUrl
            },
            Variants = matchingPlatform is not null
            ?
            [
                new Manifest.Variant
                {
                // Use matching platform information.
                Platform = GetRIDFromGo(matchingPlatform.GOARCH, matchingPlatform.GOOS),
                Dependencies = matchingPlatform.Dependencies,
                PreserveFiles = matchingPlatform.Files?.Preserve,
                RemoveFiles = matchingPlatform.Files?.Remove,
                Assets = !string.IsNullOrEmpty(matchingPlatform.AssetUrl)
                    ?
                    [
                        new Manifest.Asset
                        {
                        Type = matchingPlatform.AssetUrl.Split('.').Last() switch
                        {
                            "tar" => Manifest.Asset.TypeEnum.Tar,
                            "tgz" => Manifest.Asset.TypeEnum.Tgz,
                            "gz" => Manifest.Asset.TypeEnum.Tgz,
                            "zip" => Manifest.Asset.TypeEnum.Zip,
                            _ => Manifest.Asset.TypeEnum.Uncompressed
                        },
                        Urls = [matchingPlatform.AssetUrl],
                        Placements = ConvertPlacements(matchingPlatform.Files?.Place ?? manifestV2.Files?.Place)
                        }
                    ]
                    : null,
                Scripts = ConvertCommands(matchingPlatform.Commands ?? manifestV2.Commands)
                }
            ]
            :
            [
                new Manifest.Variant
                {
                // Fallback to global settings when no matching platform was found.
                Platform = RuntimeInformation.RuntimeIdentifier,
                Dependencies = manifestV2.Dependencies,
                PreserveFiles = manifestV2.Files?.Preserve,
                RemoveFiles = manifestV2.Files?.Remove,
                Assets = !string.IsNullOrEmpty(manifestV2.AssetUrl)
                    ?
                    [
                        new Manifest.Asset
                        {
                        Type = manifestV2.AssetUrl.Split('.').Last() switch
                        {
                            "tar" => Manifest.Asset.TypeEnum.Tar,
                            "tgz" => Manifest.Asset.TypeEnum.Tgz,
                            "gz" => Manifest.Asset.TypeEnum.Tgz,
                            "zip" => Manifest.Asset.TypeEnum.Zip,
                            _ => Manifest.Asset.TypeEnum.Uncompressed
                        },
                        Urls = [manifestV2.AssetUrl],
                        Placements = ConvertPlacements(manifestV2.Files?.Place)
                        }
                    ]
                    : null,
                Scripts = ConvertCommands(manifestV2.Commands)
                }
            ]
        };

        return JsonSerializer.SerializeToElement(manifest);

        // Local helper function to convert file placements.
        static List<Manifest.Placement>? ConvertPlacements(List<ManifestV2.PlaceType>? places)
        {
            if (places == null)
                return null;
            var placements = new List<Manifest.Placement>();
            foreach (var place in places)
            {
                placements.Add(new Manifest.Placement
                {
                    Src = place.Src.TrimEnd('*'),
                    Dest = place.Dest,
                    // If the source ends with '*', treat it as a directory; otherwise as a file.
                    Type = place.Src.EndsWith('*') ? Manifest.Placement.TypeEnum.Dir : Manifest.Placement.TypeEnum.File
                });
            }
            return placements;
        }

        // Local helper function to convert commands to scripts.
        static Manifest.ScriptsType? ConvertCommands(ManifestV2.CommandsType? commands)
        {
            if (commands == null)
                return null;
            return new Manifest.ScriptsType
            {
                PreInstall = commands.PreInstall,
                PostInstall = commands.PostInstall,
                PreUninstall = commands.PreUninstall,
                PostUninstall = commands.PostUninstall
            };
        }
    }

    private static string? GetRIDFromGo(string? goarch, string goos)
    {
        return goos switch
        {
            "windows" => goarch switch
            {
                "amd64" => "win-x64",
                "386" => "win-x86",
                "arm64" => "win-arm64",
                _ => null,
            },
            "darwin" => goarch switch
            {
                "amd64" => "osx-x64",
                "arm64" => "osx-arm64",
                _ => null,
            },
            "linux" => goarch switch
            {
                "amd64" => "linux-x64",
                "arm" => "linux-arm",
                "arm64" => "linux-arm64",
                "loong64" => "linux-loongarch64",
                _ => null,
            },
            "ios" => goarch switch
            {
                "arm64" => "ios-arm64",
                _ => null,
            },
            "android" => goarch switch
            {
                "arm" => "android-arm",
                "arm64" => "android-arm64",
                "x86" => "android-x86",
                "x64" => "android-x64",
                _ => null,
            },
            _ => null,
        };
    }
}
