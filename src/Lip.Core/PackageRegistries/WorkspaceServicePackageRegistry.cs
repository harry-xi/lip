using Lip.Core.Entities;
using Lip.Core.Services;
using Semver;

namespace Lip.Core.PackageRegistries;

public class WorkspaceServicePackageRegistry(IWorkspaceService workspaceService) : IPackageRegistry
{
    private readonly IWorkspaceService _workspaceService = workspaceService;

    public async Task<IOrderedEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId)
    {
        if ((await _workspaceService.GetInstalledPackages(IWorkspaceService.PackageScope.All))
            .SingleOrDefault(p => p.Id == packageId) is not PackageSpec packageSpec)
        {
            return Enumerable.Empty<SemVersion>().Order(SemVersion.PrecedenceComparer);

        }

        return new[] { packageSpec.Version }.Order(SemVersion.PrecedenceComparer);
    }

    public async Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec)
    {
        return await _workspaceService.GetInstalledPackageManifest(packageSpec);
    }
}