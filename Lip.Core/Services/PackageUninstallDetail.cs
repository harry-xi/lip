using Semver;
using System.Diagnostics.CodeAnalysis;

namespace Lip.Core.Services;

[ExcludeFromCodeCoverage]
internal record PackageUninstallDetail : TopoSortedPackageList<PackageUninstallDetail>.IItem
{
    public Dictionary<PackageIdentifier, SemVersionRange> Dependencies => Package.Variant.Dependencies;

    public required PackageLock.Package Package { get; init; }

    public PackageSpecifier Specifier => Package.Specifier;
}