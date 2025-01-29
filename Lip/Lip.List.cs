using System.Runtime.InteropServices;

namespace Lip;

public partial class Lip
{
    public record ListItem
    {
        public required PackageManifest Manifest { get; init; }
        public required bool Locked { get; init; }
    }

    public record ListArgs { }

    public async Task<List<ListItem>> List(ListArgs args)
    {
        PackageLock packageLock = await GetCurrentPackageLock();

        List<ListItem> listItems = [.. packageLock.Packages
            .Select(package => new ListItem
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
