using System.Runtime.InteropServices;
using Algorithms.Graphs;
using DataStructures.Graphs;

namespace Lip;

/// <summary>
/// Represents a list of packages that are topologically sorted, where dependencies are placed before dependents.
/// </summary>
public class TopoSortedPackageList<T> : List<T> where T : TopoSortedPackageList<T>.ItemType
{
    public record ItemType: IComparable<ItemType>
    {
        public required PackageManifest PackageManifest { get; init; }
        public required string VariantLabel { get; init; }

        public int CompareTo(ItemType? other) => throw new NotImplementedException();
    }

    public new T this[int index]
    {
        get => base[index];
        set
        {
            base[index] = value;
            TopoSort();
        }
    }

    public TopoSortedPackageList() : base()
    {
    }

    public TopoSortedPackageList(IEnumerable<T> collection) : base(collection)
    {
        TopoSort();
    }

    public new void Add(T item)
    {
        base.Add(item);
        TopoSort();
    }

    public new void AddRange(IEnumerable<T> collection)
    {
        base.AddRange(collection);
        TopoSort();
    }

    public new void Insert(int index, T item)
    {
        base.Insert(index, item);
        TopoSort();
    }

    public new void InsertRange(int index, IEnumerable<T> collection)
    {
        base.InsertRange(index, collection);
        TopoSort();
    }

    public new void Reverse() => throw new NotSupportedException();

    public new void Reverse(int index, int count) => throw new NotSupportedException();

    public new void Sort() => throw new NotSupportedException();

    public new void Sort(IComparer<T>? comparer) => throw new NotSupportedException();

    public new void Sort(int index, int count, IComparer<T>? comparer)
        => throw new NotSupportedException();

    public new void Sort(Comparison<T> comparison) => throw new NotSupportedException();

    private void TopoSort()
    {
        DirectedSparseGraph<T> dependencyGraph = new();

        dependencyGraph.AddVertices([.. this.Cast<T>()]);

        // Add edges.
        foreach (T item in dependencyGraph.Vertices)
        {
            IEnumerable<T> dependencies = item.PackageManifest.GetSpecifiedVariant(
                item.VariantLabel,
                RuntimeInformation.RuntimeIdentifier)?
                .Dependencies?
                .Select(dep => PackageSpecifierWithoutVersion.Parse(dep.Key))
                .Select(dep => dependencyGraph.Vertices.FirstOrDefault(v => v.PackageManifest.ToothPath == dep.ToothPath
                                                                            && v.VariantLabel == dep.VariantLabel))
                .Where(dep => dep is not null)
                .Cast<T>() ?? [];

            foreach (T dependency in dependencies)
            {
                dependencyGraph.AddEdge(item, dependency);
            }
        }

        // Topologically sort the graph.
        IEnumerable<T> sortedElements = TopologicalSorter.Sort(dependencyGraph);

        // Update the list. Note that the order is reversed.
        Clear();
        base.AddRange(sortedElements.Reverse());
    }
}
