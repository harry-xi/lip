using Lip.Core.Entities;
using Lip.Core.PackageRegistries;

using Semver;

namespace Lip.Core.Services;

public interface IDependencySolver
{
    Task<IEnumerable<PackageSpec>> Solve(IEnumerable<PackageReqt> requirements);

    /// <returns>
    /// A topologically sorted list of packages to install, where dependencies appear before the
    /// packages that depend on them.
    /// </returns>
    static IEnumerable<PackageSpec> TopologicalSort(IEnumerable<DependencyNode> nodes)
    {
        Dictionary<PackageId, DependencyNode> nodesById = nodes.ToDictionary(n => n.Spec.Id);
        HashSet<PackageId> visited = [];
        HashSet<PackageId> cycleStack = [];
        List<PackageSpec> sorted = [];

        foreach (DependencyNode node in nodes)
        {
            Visit(node);
        }

        return sorted;

        void Visit(DependencyNode node)
        {
            if (visited.Contains(node.Spec.Id))
            {
                return;
            }

            if (!cycleStack.Add(node.Spec.Id))
            {
                throw new InvalidOperationException($"Circular dependency detected involving package '{node.Spec.Id}'");
            }

            foreach (PackageReqt reqt in node.Reqts)
            {
                if (nodesById.TryGetValue(reqt.Id, out DependencyNode? depNode))
                {
                    Visit(depNode);
                }
            }

            cycleStack.Remove(node.Spec.Id);
            visited.Add(node.Spec.Id);
            sorted.Add(node.Spec);
        }
    }
}

public class DependencySolver(IPackageRegistry packageRegistry) : IDependencySolver
{
    private readonly IPackageRegistry _packageRegistry = packageRegistry;

    public async Task<IEnumerable<PackageSpec>> Solve(IEnumerable<PackageReqt> requirements)
    {
        Dictionary<PackageId, HashSet<SemVersion>> candidates = [];

        foreach (PackageReqt req in requirements)
        {
            IEnumerable<SemVersion> availableVersions = await _packageRegistry.GetAvailableVersions(req.Id);
            HashSet<SemVersion> compatibleVersions = [.. availableVersions.Where(req.VersionRange.Contains)];

            if (candidates.TryGetValue(req.Id, out HashSet<SemVersion>? value))
            {
                // Pick the intersection of existing candidates and new constraints.
                value.RemoveWhere(v => !req.VersionRange.Contains(v));
            }
            else
            {
                candidates[req.Id] = compatibleVersions;
            }
        }

        HashSet<PackageId> roots = [.. requirements.Select(x => x.Id)];

        Dictionary<PackageId, SemVersion> solution = await Backtrack(candidates, [], roots)
            ?? throw new InvalidOperationException("Cannot find a valid state to satisfy all dependencies.");

        List<DependencyNode> nodes = [];
        foreach (KeyValuePair<PackageId, SemVersion> kvp in solution)
        {
            PackageSpec spec = new(kvp.Key, kvp.Value);
            IEnumerable<PackageReqt> deps = await GetDependencies(spec);

            nodes.Add(new DependencyNode(spec, deps));
        }

        return IDependencySolver.TopologicalSort(nodes);
    }

    private async Task<Dictionary<PackageId, SemVersion>?> Backtrack(
        Dictionary<PackageId, HashSet<SemVersion>> candidates,
        Dictionary<PackageId, SemVersion> selected,
        HashSet<PackageId> roots)
    {
        if (candidates.Count == 0)
        {
            return selected;
        }

        // Fail-first heuristic: Pick candidate with fewest version options.
        (PackageId? nextId, HashSet<SemVersion>? versions) = candidates.MinBy(x => x.Value.Count);

        IOrderedEnumerable<SemVersion> sortedVersions = versions
            .Order(SemVersion.PrecedenceComparer);

        // Prefer newest versions.
        foreach (SemVersion? version in sortedVersions.Reverse())
        {
            Dictionary<PackageId, SemVersion> nextSelected = new(selected) { [nextId] = version };

            Dictionary<PackageId, HashSet<SemVersion>> nextCandidates = new(candidates);
            nextCandidates.Remove(nextId);

            if (await TryPropagate(nextId, version, nextSelected, nextCandidates))
            {
                Dictionary<PackageId, SemVersion>? result = await Backtrack(nextCandidates, nextSelected, roots);
                if (result is not null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    private async Task<bool> TryPropagate(
        PackageId packageId,
        SemVersion version,
        Dictionary<PackageId, SemVersion> currentSelection,
        Dictionary<PackageId, HashSet<SemVersion>> currentCandidates)
    {
        IEnumerable<PackageReqt> dependencies = await GetDependencies(new PackageSpec(packageId, version));

        foreach (PackageReqt req in dependencies)
        {
            // Conflict with already selected package?
            if (currentSelection.TryGetValue(req.Id, out SemVersion? selectedVer))
            {
                if (!req.VersionRange.Contains(selectedVer))
                {
                    return false;

                }

                continue; // Constraint satisfied.
            }

            // Constrain remaining candidates
            if (currentCandidates.TryGetValue(req.Id, out HashSet<SemVersion>? options))
            {
                options.RemoveWhere(v => !req.VersionRange.Contains(v));

                if (options.Count == 0)
                {
                    return false;
                }
            }
            else
            {
                // Discovery of new dependency
                IEnumerable<SemVersion> available = await _packageRegistry.GetAvailableVersions(req.Id);
                HashSet<SemVersion> compatible = [.. available.Where(req.VersionRange.Contains)];

                if (compatible.Count == 0)
                {
                    return false;
                }
                currentCandidates[req.Id] = compatible;
            }
        }

        return true;
    }

    private async Task<IEnumerable<PackageReqt>> GetDependencies(PackageSpec spec)
    {
        PackageManifest manifest = await _packageRegistry.GetPackageManifest(spec);
        PackageManifestVariant variant = manifest.GetVariant(spec.Id.Variant);
        return variant.Dependencies.Select(kv => new PackageReqt(kv.Key, kv.Value));
    }
}