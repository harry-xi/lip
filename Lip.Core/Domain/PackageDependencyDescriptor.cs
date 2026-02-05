using Semver;

namespace Lip.Core;

public record PackageDependencyDescriptor(
    PackageSpecifier Specifier,
    IEnumerable<PackageRequirement> Dependencies);