using Flurl;
using Lip.Core.Entities;
using Lip.Core.Infrastructure;
using Lip.Core.Services;
using Semver;
using System.IO.Abstractions;

namespace Lip.Core.PackageRegistries;

public class LiprPackageRegistry(IFileDownloader fileDownloader, ICacheService cacheService) : IPackageRegistry
{
    private readonly IFileDownloader _fileDownloader = fileDownloader;

    private readonly ICacheService _cacheService = cacheService;

    public async Task<IOrderedEnumerable<SemVersion>> GetAvailableVersions(PackageId packageId)
    {
        throw new NotSupportedException();
    }

    public async Task<PackageManifest> GetPackageManifest(PackageSpec packageSpec)
    {
        Url url = Url.Parse(
            $"https://lipr.levimc.org/{packageSpec.Id.Path}/{packageSpec.Version}/tooth.json");

        IFileInfo file = await _cacheService.GetOrCreateFile(url, async cacheFile =>
        {
            await _fileDownloader.DownloadFile(url, cacheFile);
        });

        using Stream manifestStream = file.OpenRead();

        return await PackageManifest.FromStream(manifestStream);
    }
}