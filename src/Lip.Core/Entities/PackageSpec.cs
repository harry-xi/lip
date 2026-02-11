using Semver;
using System.Text.RegularExpressions;

namespace Lip.Core.Entities;

public partial record PackageSpec(PackageId Id, SemVersion Version)
{
    public PackageId Id { get; init; } = Id;

    public SemVersion Version { get; init; } = Version;

    public override string ToString() => $"{Id}@{Version}";

    public static PackageSpec Parse(string s)
    {
        Match match = SelfRegex().Match(s);
        if (!match.Success)
        {
            throw new FormatException($"Invalid package spec: {s}");
        }

        string id = match.Groups["id"].Value;
        string version = match.Groups["version"].Value;

        return new PackageSpec(PackageId.Parse(id), SemVersion.Parse(version));
    }

    [GeneratedRegex(@"^(?<id>[^@]+)@(?<version>[^@]+)$")]
    private static partial Regex SelfRegex();
}