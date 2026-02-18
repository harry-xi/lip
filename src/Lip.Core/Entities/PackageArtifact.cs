using Lip.Core.Sources;

namespace Lip.Core.Entities;

public record PackageArtifact(
    PackageSpec Spec,
    ISource Source);