using Semver;

namespace Lip.Core;

public static class TopologicalSort
{
    public static List<PackageDependencyDescriptor> Sort(IEnumerable<PackageDependencyDescriptor> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return [];

        var duplicateGroups = itemList
            .GroupBy(i => i.Specifier.Identifier)
            .Where(g => g.Skip(1).Any())
            .ToList();
        if (duplicateGroups.Count > 0)
        {
            var duplicateIds = string.Join(", ", duplicateGroups.Select(g => g.Key.ToString()));
            throw new InvalidOperationException(
                $"Topological sort contains multiple items with the same PackageIdentifier: {duplicateIds}.");
        }

        var itemMap = itemList.ToDictionary(i => i.Specifier.Identifier);
        var visited = new HashSet<PackageIdentifier>();
        var visiting = new HashSet<PackageIdentifier>();
        var sorted = new List<PackageDependencyDescriptor>(itemList.Count);

        void Visit(PackageDependencyDescriptor item)
        {
            var id = item.Specifier.Identifier;
            if (visited.Contains(id)) return;
            if (visiting.Contains(id)) return; // Cycle detected, break to proceed with partial ordering.

            visiting.Add(id);

            foreach (var dep in item.Dependencies)
            {
                if (itemMap.TryGetValue(dep.Key, out var depItem) &&
                    dep.Value.Contains(depItem.Specifier.Version))
                {
                    Visit(depItem);
                }
            }

            visiting.Remove(id);
            visited.Add(id);
            sorted.Add(item);
        }

        foreach (var item in itemList)
        {
            Visit(item);
        }

        // DFS post-order gives [Leaf, ..., Root]. We need [Root, ..., Leaf].
        sorted.Reverse();
        return sorted;
    }
}