using Golang.Org.X.Mod;
using System.Text.RegularExpressions;

namespace Lip.Core;

public record PackageIdentifier
{
    public required string ToothPath
    {
        get => _toothPath;
        init
        {
            if (!IsValidToothPath(value))
            {
                throw new ArgumentException($"Invalid tooth path {value}.", nameof(ToothPath));
            }

            _toothPath = value;
        }
    }
    private readonly string _toothPath = string.Empty;

    public required string VariantLabel
    {
        get => _variantLabel;
        init
        {
            if (!IsValidVariantLabel(value))
            {
                throw new ArgumentException($"Invalid variant label {value}.", nameof(VariantLabel));
            }

            _variantLabel = value;
        }
    }
    private string _variantLabel = string.Empty;

    public static PackageIdentifier Parse(string text)
    {
        if (!IsValid(text))
        {
            throw new ArgumentException($"Invalid package identifier '{text}'.", nameof(text));
        }

        string[] parts = text.Split('#');

        return new PackageIdentifier
        {
            ToothPath = parts[0],
            VariantLabel = parts.ElementAtOrDefault(1) ?? string.Empty
        };
    }

    public override string ToString()
    {
        return $"{ToothPath}{(VariantLabel != string.Empty ? "#" : string.Empty)}{VariantLabel}";
    }


    /// <summary>
    /// Checks if the package identifier is valid.
    /// </summary>
    /// <param name="packageIdentifier">The package identifier to validate.</param>
    /// <returns>True if the package identifier is valid; otherwise, false.</returns>
    public static bool IsValid(string packageIdentifier)
    {
        // Split the package specifier into tooth path and variant label.
        string[] toothPathAndVariantLabel = packageIdentifier.Split('#');

        if (toothPathAndVariantLabel.Length > 2)
        {
            return false;
        }

        string toothPath = toothPathAndVariantLabel[0];
        if (!IsValidToothPath(toothPath))
        {
            return false;
        }

        if (toothPathAndVariantLabel.Length == 2)
        {
            string variantLabel = toothPathAndVariantLabel[1];
            if (!IsValidVariantLabel(variantLabel))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the tooth path is valid.
    /// </summary>
    /// <param name="toothPath">The tooth path to validate.</param>
    /// <returns>True if the tooth path is valid; otherwise, false.</returns>
    public static bool IsValidToothPath(string toothPath)
    {
        return Module.CheckPath(toothPath) is null;
    }

    /// <summary>
    /// Checks if the variant label is valid.
    /// </summary>
    /// <param name="variantLabel">The variant label to validate.</param>
    /// <returns>True if the variant label is valid; otherwise, false.</returns>
    public static bool IsValidVariantLabel(string variantLabel)
    {
        return new Regex("^([a-z0-9]+(_[a-z0-9]+)*)?$").IsMatch(variantLabel);
    }
}