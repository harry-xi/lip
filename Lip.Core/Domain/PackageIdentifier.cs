using Golang.Org.X.Mod;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Lip.Core;

[JsonConverter(typeof(JsonConverters.PackageIdentifierConverter))]
public partial record PackageIdentifier(string ToothPath, string VariantLabel = "")
{
    public string ToothPath { get; init; } = IsValidToothPath(ToothPath)
        ? ToothPath
        : throw new ArgumentException($"Invalid tooth path {ToothPath}.", nameof(ToothPath));

    public string VariantLabel { get; init; } = IsValidVariantLabel(VariantLabel)
        ? VariantLabel
        : throw new ArgumentException($"Invalid variant label {VariantLabel}.", nameof(VariantLabel));

    public override string ToString() => $"{ToothPath}{(VariantLabel != string.Empty ? "#" : string.Empty)}{VariantLabel}";

    public static PackageIdentifier Parse(string text)
    {
        if (!TryParse(text, out PackageIdentifier? result))
        {
            throw new FormatException($"Invalid package identifier '{text}'.");
        }
        return result;
    }

    public static bool TryParse(string text, [NotNullWhen(true)] out PackageIdentifier? result)
    {
        result = null;
        Match match = IdentifierRegex().Match(text);
        if (!match.Success)
        {
            return false;
        }

        string toothPath = match.Groups["path"].Value;
        string variantLabel = match.Groups["label"].Value;

        if (!IsValidToothPath(toothPath) || !IsValidVariantLabel(variantLabel))
        {
            return false;
        }

        result = new PackageIdentifier(toothPath, variantLabel);
        return true;
    }

    public static bool IsValidToothPath(string toothPath)
    {
        return Module.CheckPath(toothPath) is null;
    }

    public static bool IsValidVariantLabel(string variantLabel)
    {
        return VariantLabelRegex().IsMatch(variantLabel);
    }

    [GeneratedRegex(@"^(?<path>[^#]+)(?:#(?<label>[^#]*))?$")]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex("^([a-z0-9]+(_[a-z0-9]+)*)?$")]
    private static partial Regex VariantLabelRegex();
}