using Flurl;
using Flurl.Http;
using Semver;

namespace Lip.Core.PackageRegistries;

public class LiprRegistry : IPackageRegistry
{
    public async Task<PackageManifest> GetManifest(PackageSpecifier packageSpecifier)
    {
        Url manifestUrl = Url.Parse($"https://lipr.levimc.org/{packageSpecifier.ToothPath}/v{packageSpecifier.Version}/tooth.json");

        using Stream manifestStream = await manifestUrl.GetStreamAsync();

        return await PackageManifest.FromStream(manifestStream);
    }

    public async Task<List<SemVersion>> GetVersions(PackageIdentifier packageIdentifier)
    {
        throw new NotImplementedException();
    }
}