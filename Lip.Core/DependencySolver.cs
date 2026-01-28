using Microsoft.Extensions.Logging;
using Semver;
using System.Runtime.InteropServices;

namespace Lip.Core;

public interface IDependencySolver
{
    Task<List<PackageSpecifier>?> ResolveDependencies(
        IEnumerable<PackageSpecifier> primaryPackageSpecifiers,
        IEnumerable<PackageLock.Package> knownPackages);
}

public class DependencySolver(IContext context, IPackageManager packageManager) : IDependencySolver
{
    private readonly IContext _context = context;
    private readonly IPackageManager _packageManager = packageManager;


    public async Task<List<PackageSpecifier>?> ResolveDependencies(
        IEnumerable<PackageSpecifier> primaryPackageSpecifiers,
        IEnumerable<PackageLock.Package> knownPackages)
    {
        _context.Logger.LogDebug("Resolving dependencies...");

        Dictionary<PackageIdentifier, HashSet<SemVersion>> candidates = primaryPackageSpecifiers.ToDictionary(
            static ps => ps.Identifier,
            static ps => new HashSet<SemVersion> { ps.Version }
        );

        Dictionary<PackageIdentifier, SemVersion> selected = [];

        return await Backtrack(candidates, selected, knownPackages)
            ? [.. selected.Select(static kv => PackageSpecifier.FromIdentifier(kv.Key, kv.Value))]
            : throw new InvalidOperationException("Cannot find a valid state to satisfy all dependencies.");
    }

    private async Task<bool> Backtrack(
        Dictionary<PackageIdentifier, HashSet<SemVersion>> candidates,
        Dictionary<PackageIdentifier, SemVersion> selected,
        IEnumerable<PackageLock.Package> knownPackages)
    {
        // 1. Base case: All candidates resolved
        if (candidates.Count == 0)
        {
            return true;
        }

        // 2. Heuristic: Pick candidate with fewest version options (Fail-First)
        (PackageIdentifier nextId, HashSet<SemVersion> versions) = candidates.MinBy(x => x.Value.Count);

        // 3. Sort versions: Prefer lower versions
        List<SemVersion> sortedVersions = [.. versions
                .OrderBy(v => v, SemVersion.PrecedenceComparer)];

        foreach (SemVersion? version in sortedVersions)
        {
            // Create next state (Copy-On-Write)
            Dictionary<PackageIdentifier, SemVersion> nextSelected = new(selected)
            {
                [nextId] = version
            };

            Dictionary<PackageIdentifier, HashSet<SemVersion>> nextCandidates = new(candidates);
            _ = nextCandidates.Remove(nextId);

            bool isValidBranch = true;

            try
            {
                Dictionary<PackageIdentifier, SemVersionRange> dependencies = await GetDependencies(PackageSpecifier.FromIdentifier(nextId, version), _packageManager, knownPackages);

                foreach ((PackageIdentifier depId, SemVersionRange range) in dependencies)
                {
                    // Check against already selected packages
                    if (nextSelected.TryGetValue(depId, out SemVersion? existingVer))
                    {
                        if (!range.Contains(existingVer))
                        {
                            isValidBranch = false;
                            break;
                        }
                    }
                    else
                    {
                        // Propagate constraints to candidates
                        if (nextCandidates.TryGetValue(depId, out HashSet<SemVersion>? currentOptions))
                        {
                            HashSet<SemVersion> intersection = [.. currentOptions.Where(range.Contains)];
                            if (intersection.Count == 0)
                            {
                                isValidBranch = false;
                                break;
                            }
                            nextCandidates[depId] = intersection;
                        }
                        else
                        {
                            // New dependency discovered
                            HashSet<SemVersion> availableVersions = await FetchAvailableVersions(depId, knownPackages);
                            HashSet<SemVersion> compatibleVersions = [.. availableVersions.Where(range.Contains)];

                            if (compatibleVersions.Count == 0)
                            {
                                isValidBranch = false;
                                break;
                            }
                            nextCandidates[depId] = compatibleVersions;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If fetching dependencies fails, this branch is invalid
                isValidBranch = false;
            }

            if (isValidBranch)
            {
                // Recurse
                if (await Backtrack(nextCandidates, nextSelected, knownPackages))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private async Task<HashSet<SemVersion>> FetchAvailableVersions(PackageIdentifier id, IEnumerable<PackageLock.Package> known)
    {
        IEnumerable<SemVersion> localVersions = known
            .Where(p => p.Specifier.Identifier == id)
            .Select(p => p.Specifier.Version);

        HashSet<SemVersion> result = [.. localVersions];

        try
        {
            List<SemVersion> remoteVersions = await _packageManager.GetPackageRemoteVersions(id);
            if (remoteVersions != null)
            {
                result.UnionWith(remoteVersions);
            }
        }
        catch
        {
            // Ignore remote fetch errors, proceed with known versions
        }

        return result;
    }

    private static async Task<Dictionary<PackageIdentifier, SemVersionRange>> GetDependencies(
        PackageSpecifier packageSpecifier,
        IPackageManager packageManager,
        IEnumerable<PackageLock.Package> knownPackages
    )
    {
        if (knownPackages.FirstOrDefault(p => p.Specifier == packageSpecifier) is { } knownPkg)
        {
            return knownPkg.Variant.Dependencies;
        }

        PackageManifest? manifest = await packageManager.GetPackageManifestFromCache(packageSpecifier) ?? throw new InvalidOperationException($"Failed to get package manifest for {packageSpecifier}.");
        PackageManifest.Variant? variant = manifest.GetVariant(packageSpecifier.VariantLabel, RuntimeInformation.RuntimeIdentifier);
        return variant == null
            ? throw new InvalidOperationException($"Variant {packageSpecifier.VariantLabel} not found for {packageSpecifier}.")
            : variant.Dependencies;
    }
}