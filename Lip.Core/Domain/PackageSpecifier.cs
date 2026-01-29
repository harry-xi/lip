using Semver;

namespace Lip.Core;


public record PackageSpecifier
{
    public PackageIdentifier Identifier => new()
    {
        ToothPath = ToothPath,
        VariantLabel = VariantLabel
    };

    public required string ToothPath
    {
        get => _toothPath;
        init
        {
            if (!PackageIdentifier.IsValidToothPath(value))
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
            if (!PackageIdentifier.IsValidVariantLabel(value))
            {
                throw new ArgumentException($"Invalid variant label {value}.", nameof(VariantLabel));
            }

            _variantLabel = value;
        }
    }
    private string _variantLabel = string.Empty;

    public required SemVersion Version { get; init; }

    public static PackageSpecifier FromIdentifier(PackageIdentifier identifier, SemVersion version)
    {
        return new PackageSpecifier
        {
            ToothPath = identifier.ToothPath,
            VariantLabel = identifier.VariantLabel,
            Version = version
        };
    }

    public static PackageSpecifier Parse(string text)
    {
        if (!IsValid(text))
        {
            throw new ArgumentException(
                $"Invalid package specifier '{text}'. Expected format is 'toothPath[#variantLabel]@version', e.g. 'example.com/user/repo@1.0.0'.", nameof(text));
        }

        string[] parts = text.Split('@');

        PackageIdentifier packageIdentifier = PackageIdentifier.Parse(parts[0]);

        return new PackageSpecifier
        {
            ToothPath = packageIdentifier.ToothPath,
            VariantLabel = packageIdentifier.VariantLabel,
            Version = SemVersion.Parse(parts[1])
        };
    }

    public override string ToString()
    {
        string packageIdentifierText = new PackageIdentifier
        {
            ToothPath = ToothPath,
            VariantLabel = VariantLabel
        }.ToString();

        return $"{packageIdentifierText}@{Version}";
    }


    /// <summary>
    /// Checks if the package specifier is valid.
    /// </summary>
    /// <param name="packageSpecifier">The package specifier to validate.</param>
    /// <returns>True if the package specifier is valid; otherwise, false.</returns>
    public static bool IsValid(string packageSpecifier)
    {
        // Split the package specifier, whose first part is tooth path + variant label and the second part is version.
        string[] parts = packageSpecifier.Split('@');
        if (parts.Length != 2)
        {
            return false;
        }

        string version = parts[1];
        if (!SemVersion.TryParse(version, out _))
        {
            return false;
        }

        return PackageIdentifier.IsValid(parts[0]);
    }
}