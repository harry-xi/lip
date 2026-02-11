namespace Lip.Core.Entities;

public record DependencyNode(
    PackageSpec Spec,
    IEnumerable<PackageReqt> Reqts);