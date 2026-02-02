using System.Runtime.InteropServices;

namespace Lip.Core;

public partial record PackageLock
{
    public record Package
    {
        public required List<string> Files { get; init; }

        public required bool Locked { get; init; }

        public required PackageManifest Manifest { private get; init; }

        public PackageSpecifier Specifier => new(new PackageIdentifier(Manifest.ToothPath, VariantLabel), Manifest.Version);

        public PackageManifest.Variant Variant => Manifest.GetVariant(
            VariantLabel,
            RuntimeInformation.RuntimeIdentifier)!;

        public required string VariantLabel
        {
            private get => _variantLabel;
            init
            {
                if (!PackageIdentifier.IsValidVariantLabel(value))
                {
                    throw new SchemaViolationException(
                        "packages[].variant_format",
                        $"Variant label '{value}' does not meet the required format."
                    );
                }
                if (Manifest.GetVariant(value, RuntimeInformation.RuntimeIdentifier) is null)
                {
                    throw new SchemaViolationException(
                        "packages[].variant",
                        $"No matching variant found for label '{value}' with runtime identifier '{RuntimeInformation.RuntimeIdentifier}'."
                    );
                }
                _variantLabel = value;
            }
        }
        private readonly string _variantLabel = string.Empty;
    }
}