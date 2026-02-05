using Semver;

namespace Lip.Core.Entities;

public record PackageRequirement(PackageId Id, SemVersionRange Version);