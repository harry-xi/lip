using Lip.Core.SourceProviders;

namespace Lip.Core.Entities;

public record PackageArtifact(
    PackageSpec Spec,
    ISourceProvider SourceProvider);