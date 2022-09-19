using System;
using System.Collections.Generic;

namespace GitObjectDb.Comparison;

internal sealed class MergeChangeEqualityComparer : IEqualityComparer<MergeChange>
{
    private MergeChangeEqualityComparer()
    {
    }

    public static MergeChangeEqualityComparer Instance { get; } = new MergeChangeEqualityComparer();

    public bool Equals(MergeChange x, MergeChange y)
    {
        var xItem = ExtracItemData(x);
        var yItem = ExtracItemData(y);
        return xItem.Equals(yItem);
    }

    public int GetHashCode(MergeChange obj)
    {
        return ExtracItemData(obj).GetHashCode();
    }

    private static object ExtracItemData(MergeChange obj)
    {
        var item = obj.Theirs ?? obj.Ours;
        return item switch
        {
            Node node => node.Id,
            Resource resource => resource.Path,
            _ => throw new NotSupportedException(),
        };
    }
}
