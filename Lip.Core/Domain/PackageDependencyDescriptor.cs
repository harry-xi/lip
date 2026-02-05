using Semver;

namespace Lip.Core;

public record PackageDependencyDescriptor(
    PackageSpecifier Specifier,
    IDictionary<PackageIdentifier, SemVersionRange> Dependencies);