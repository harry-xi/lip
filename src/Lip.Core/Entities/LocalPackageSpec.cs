using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace Lip.Core.Entities;

public partial record LocalPackageSpec(
    IFileInfo ArchiveFile,
    string Variant)
{
    public IFileInfo ArchiveFile { get; init; } = ArchiveFile;

    public string Variant { get; init; } = PackageId.IsValidVariant(Variant)
        ? Variant
        : throw new FormatException($"Invalid package variant: {Variant}");

    public override string ToString() => $"{ArchiveFile.FullName}{(Variant != string.Empty ? "#" : string.Empty)}{Variant}";

    public static LocalPackageSpec Parse(string s, IFileSystem fileSystem)
    {
        Match match = SelfRegex().Match(s);
        if (!match.Success)
        {
            throw new FormatException($"Invalid local package spec: {s}");
        }

        string path = match.Groups["path"].Value;
        string variant = match.Groups["label"].Value;

        IFileInfo archiveFile = fileSystem.FileInfo.New(path);
        if (!archiveFile.Exists)
        {
            throw new FileNotFoundException($"Archive file not found: {path}");
        }

        return new LocalPackageSpec(archiveFile, variant);
    }

    [GeneratedRegex(@"^(?<path>[^#]+)(?:#(?<label>[^#]*))?$")]
    private static partial Regex SelfRegex();
}