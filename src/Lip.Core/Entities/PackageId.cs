using System.Text.RegularExpressions;

namespace Lip.Core.Entities;

public partial record PackageId(string Path, string Variant)
{
    public string Path { get; init; } = (Golang.Org.X.Mod.Module.CheckPath(Path) is null)
        ? Path
        : throw new FormatException($"Invalid package path: {Path}");

    public string Variant { get; init; } = IsValidVariant(Variant)
        ? Variant
        : throw new FormatException($"Invalid package variant: {Variant}");

    public override string ToString() => $"{Path}{(Variant != string.Empty ? "#" : string.Empty)}{Variant}";

    public static bool IsValidVariant(string variant) => VariantRegex().IsMatch(variant);

    public static PackageId Parse(string s)
    {
        Match match = SelfRegex().Match(s);
        if (!match.Success)
        {
            throw new FormatException($"Invalid package id: {s}");
        }

        string path = match.Groups["path"].Value;
        string variant = match.Groups["label"].Value;

        return new PackageId(path, variant);
    }

    [GeneratedRegex(@"^(?<path>[^#]+)(?:#(?<label>[^#]*))?$")]
    private static partial Regex SelfRegex();

    [GeneratedRegex(@"^([a-z0-9_]+(/[a-z0-9_]+)?)?$")]
    private static partial Regex VariantRegex();
}