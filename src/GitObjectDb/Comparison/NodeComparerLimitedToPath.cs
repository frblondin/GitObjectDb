using ObjectsComparer;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Comparison;
internal class NodeComparerLimitedToPath : AbstractValueComparer, IEqualityComparer<Node>
{
    public static NodeComparerLimitedToPath Instance { get; } = new NodeComparerLimitedToPath();

    public override bool Compare(object obj1, object obj2, ComparisonSettings settings)
    {
        if (obj1 == obj2)
        {
            return true;
        }

        return obj1 switch
        {
            Node node1 when obj2 is Node node2 => Equals(node1, node2),
            IEnumerable<Node> nodeEnum1 when obj2 is IEnumerable<Node> nodeEnum2 => nodeEnum1.OrderBy(n => n.Path)
                .SequenceEqual(nodeEnum2.OrderBy(n => n.Path), this),
            _ => false,
        };
    }

    public bool Equals(Node x, Node y) => x.Path?.Equals(y.Path) ?? false;

    public int GetHashCode(Node obj) => obj?.Path?.GetHashCode() ?? 0;
}
