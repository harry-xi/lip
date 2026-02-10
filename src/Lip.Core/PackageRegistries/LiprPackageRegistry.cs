using Flurl;
using Flurl.Http;
using Lip.Core.Entities;
using Semver;
using System.Text.Json;

namespace Lip.Core.PackageRegistries;

public class LiprPackageRegistry : IPackageRegistry
{
    public async Task<IOrderedEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId)
    {
        throw new NotSupportedException();
    }

    public async Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec)
    {
        Url url = Url.Parse(
            $"https://lipr.levimc.org/{packageSpec.Id.Path}/v{packageSpec.Version}/tooth.json");

        using Stream manifestStream = await url.GetStreamAsync();

        return await PackageManifest.FromStream(manifestStream);
    }
}