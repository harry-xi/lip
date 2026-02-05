using Semver;

namespace Lip.Core;

public record PackageRequirement(
    PackageIdentifier Identifier,
    SemVersionRange VersionRange);