namespace Lip.Core.Services;

public class ListService(IPackageManager packageManager)
{
    private readonly IPackageManager _packageManager = packageManager;

    public record Args { }

    public record ResultItem
    {
        public required bool Locked { get; init; }
        public required PackageSpecifier Specifier { get; init; }
        public required PackageManifest.Variant Variant { get; init; }
    }

    public async Task<List<ResultItem>> List(Args args)
    {
        PackageLock packageLock = await _packageManager.GetCurrentPackageLock();

        List<ResultItem> listItems = packageLock.Packages
            .ConvertAll(@lock => new ResultItem
            {
                Locked = @lock.Locked,
                Specifier = @lock.Specifier,
                Variant = @lock.Variant
            });

        return listItems;
    }
}