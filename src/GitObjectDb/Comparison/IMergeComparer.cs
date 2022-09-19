using System.Collections.Generic;

namespace GitObjectDb.Comparison
{
    internal interface IMergeComparer
    {
        IEnumerable<MergeChange> Compare(ChangeCollection localChanges, ChangeCollection toBeMergedIntoLocal, ComparisonPolicy policy);
    }
}