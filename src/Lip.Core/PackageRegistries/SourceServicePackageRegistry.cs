using Lip.Core.Entities;
using Lip.Core.Services;
using Lip.Core.SourceProviders;
using Semver;
using System.Text.Json;

namespace Lip.Core.PackageRegistries;

public class SourceServicePackageRegistry(ISourceService sourceService) : IPackageRegistry
{
    private readonly ISourceService _sourceService = sourceService;

    public Task<IEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId)
    {
        throw new NotSupportedException();
    }

    public async Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec)
    {
        ISourceProvider sourceProvider = await _sourceService.Get(packageSpec);

        using Stream manifestStream = await sourceProvider.OpenRead("tooth.json");
        PackageManifest manifest = (await JsonSerializer.DeserializeAsync<PackageManifest>(manifestStream))!;

        return manifest;
    }
}