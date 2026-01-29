using Semver;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Lip.Core;

public partial record PackageSpecifier(PackageIdentifier Identifier, SemVersion Version)
{
    public string ToothPath => Identifier.ToothPath;

    public string VariantLabel => Identifier.VariantLabel;

    public override string ToString() => $"{Identifier}@{Version}";

    public static PackageSpecifier Parse(string text)
    {
        if (!TryParse(text, out PackageSpecifier? result))
        {
            throw new FormatException(
                $"Invalid package specifier '{text}'.");
        }
        return result;
    }

    public static bool TryParse(string text, [NotNullWhen(true)] out PackageSpecifier? result)
    {
        result = null;
        Match match = SpecifierRegex().Match(text);
        if (!match.Success)
        {
            return false;
        }

        if (!PackageIdentifier.TryParse(match.Groups["identifier"].Value, out PackageIdentifier? identifier))
        {
            return false;
        }

        if (!SemVersion.TryParse(match.Groups["version"].Value, out SemVersion? version))
        {
            return false;
        }

        result = new PackageSpecifier(identifier!, version!);
        return true;
    }

    [GeneratedRegex(@"^(?<identifier>[^@]+)@(?<version>[^@]+)$")]
    private static partial Regex SpecifierRegex();
}