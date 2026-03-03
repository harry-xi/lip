using Semver;

namespace Lip.Core.Entities;

public record PackageReqt(PackageId Id, SemVersionRange VersionRange) {
  public PackageId Id { get; init; } = Id;

  public SemVersionRange VersionRange { get; init; } = VersionRange;

  public override string ToString() {
    return $"{Id}@[{VersionRange}]";
  }
}