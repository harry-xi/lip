using Lip.Core.Entities;

namespace Lip.Core.Services;

public interface IDependencySolver
{
    Task<IEnumerable<PackageSpec>> Solve(IEnumerable<PackageReqt> packageReqts);

    /// <returns>
    /// A topologically sorted list of packages to install, where dependencies appear before the
    /// packages that depend on them.
    /// </returns>
    static IEnumerable<PackageSpec> TopologicalSort(IEnumerable<DependencyNode> dependencyNodes)
    {
        var nodesById = dependencyNodes.ToDictionary(n => n.Spec.Id);
        var visited = new HashSet<PackageId>();
        var visiting = new HashSet<PackageId>();
        var sorted = new List<PackageSpec>();

        void Visit(DependencyNode node)
        {
            if (visited.Contains(node.Spec.Id))
            {
                return;
            }

            if (!visiting.Add(node.Spec.Id))
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

            visiting.Remove(node.Spec.Id);
            visited.Add(node.Spec.Id);
            sorted.Add(node.Spec);
        }

        foreach (DependencyNode node in dependencyNodes)
        {
            Visit(node);
        }

        return sorted;
    }
}

public class DependencySolver : IDependencySolver
{
    public Task<IEnumerable<PackageSpec>> Solve(IEnumerable<PackageReqt> packageReqts)
    {
        throw new NotImplementedException();
    }
}