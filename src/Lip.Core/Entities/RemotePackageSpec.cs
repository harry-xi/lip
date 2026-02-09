using Flurl;
using System.Text.RegularExpressions;

namespace Lip.Core.Entities;

public partial record RemotePackageSpec(
    Url ArchiveUrl,
    string Variant)
{
    public Url ArchiveUrl { get; init; } = ArchiveUrl;

    public string Variant { get; init; } = PackageId.IsValidVariant(Variant)
        ? Variant
        : throw new FormatException($"Invalid package variant: {Variant}");

    public override string ToString() => $"{ArchiveUrl}{(Variant != string.Empty ? "#" : string.Empty)}{Variant}";

    public static RemotePackageSpec Parse(string s)
    {
        Match match = SelfRegex().Match(s);
        if (!match.Success)
        {
            throw new FormatException($"Invalid remote package spec: {s}");
        }

        string path = match.Groups["path"].Value;
        string variant = match.Groups["label"].Value;

        return new RemotePackageSpec(new Url(path), variant);
    }

    [GeneratedRegex(@"^(?<path>[^#]+)(?:#(?<label>[^#]*))?$")]
    private static partial Regex SelfRegex();
}