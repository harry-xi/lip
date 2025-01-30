using System.Runtime.InteropServices;

namespace Lip;

public partial class Lip
{
    public record ListArgs { }

    public record ListResultItem
    {
        public required PackageManifest Manifest { get; init; }
        public required bool Locked { get; init; }
    }

    public async Task<List<ListResultItem>> List(ListArgs args)
    {
        PackageLock packageLock = await GetCurrentPackageLock();

        List<ListResultItem> listItems = [.. packageLock.Packages
            .Select(package => new ListResultItem
            {
                Manifest = package,
                Locked = packageLock.Locks.Any(l =>
                {
                    return package.ToothPath == l.ToothPath
                        && package.Version == l.Version
                        && package.GetSpecifiedVariant(
                            l.VariantLabel,
                            RuntimeInformation.RuntimeIdentifier) is not null;
                })
            })];

        return listItems;
    }
}
