using Lip.Core.Entities;
using Lip.Core.Services;
using Lip.Core.Sources;
using Semver;

namespace Lip.Core.PackageRegistries;

public class SourceServicePackageRegistry(ISourceService sourceService) : IPackageRegistry
{
    private readonly ISourceService _sourceService = sourceService;

    public Task<IOrderedEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId)
    {
        throw new NotSupportedException();
    }

    public async Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec)
    {
        ISource source = await _sourceService.Get(packageSpec);

        using Stream manifestStream = await source.OpenRead("tooth.json");
        PackageManifest manifest = await PackageManifest.FromStream(manifestStream);

        return manifest;
    }
}