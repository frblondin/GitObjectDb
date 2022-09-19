using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Comparison;
internal static class MergeChangeExtensions
{
    internal static bool HasAnyConflict(this IEnumerable<MergeChange> changes) =>
        changes.Any(c => c.Status == ItemMergeStatus.EditConflict || c.Status == ItemMergeStatus.TreeConflict);
}
