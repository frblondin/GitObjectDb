using System;
using System.Collections.Generic;

namespace GitObjectDb.Comparison;

internal sealed class MergeChangeEqualityComparer : IEqualityComparer<MergeChange>
{
    private MergeChangeEqualityComparer()
    {
    }

    public static MergeChangeEqualityComparer Instance { get; } = new MergeChangeEqualityComparer();

    public bool Equals(MergeChange? x, MergeChange? y)
    {
        var xItem = ExtracItemData(x);
        var yItem = ExtracItemData(y);
        return xItem == yItem || (xItem?.Equals(yItem) ?? false);
    }

    public int GetHashCode(MergeChange obj)
    {
        return ExtracItemData(obj)?.GetHashCode() ?? 0;
    }

    private static object? ExtracItemData(MergeChange? obj)
    {
        var item = obj?.Theirs ?? obj?.Ours ?? obj?.Ancestor;
        return item switch
        {
            Node node => node.Id,
            Resource resource => resource.ThrowIfNoPath(),
            null => null,
            _ => throw new NotSupportedException(),
        };
    }
}
