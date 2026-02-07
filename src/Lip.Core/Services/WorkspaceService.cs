using Lip.Core.Entities;

namespace Lip.Core.Services;

public interface IWorkspaceService
{
    enum PackageScope
    {
        All,
        Explicit,
        Implicit,
    }

    Task<IEnumerable<PackageSpec>> GetInstalledPackages(
        PackageScope scope = PackageScope.All);
}