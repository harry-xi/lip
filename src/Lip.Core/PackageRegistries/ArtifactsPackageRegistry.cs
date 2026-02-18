using Lip.Core.Entities;
using Semver;

namespace Lip.Core.PackageRegistries;

public class ArtifactsPackageRegistry(IEnumerable<PackageArtifact> packageArtifacts) : IPackageRegistry
{
    private readonly IEnumerable<PackageArtifact> _packageArtifacts = packageArtifacts;

    public async Task<IOrderedEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId)
    {
        return _packageArtifacts
            .Where(pa => pa.Spec.Id == packageId)
            .Select(pa => pa.Spec.Version)
            .Order(SemVersion.PrecedenceComparer);
    }

    public async Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec)
    {
        return await _packageArtifacts
            .Where(pa => pa.Spec == packageSpec)
            .Select(async pa =>
            {
                using Stream stream = await pa.Source.OpenRead("tooth.json");

                return await PackageManifest.FromStream(stream);
            })
            .Single();
    }
}