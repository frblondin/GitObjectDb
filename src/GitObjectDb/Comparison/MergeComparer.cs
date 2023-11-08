using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Comparison;

internal class MergeComparer : IMergeComparer
{
    public IEnumerable<MergeChange> Compare(ChangeCollection localChanges,
                                            ChangeCollection toBeMergedIntoLocal,
                                            ComparisonPolicy policy)
    {
        return CompareImpl(localChanges, toBeMergedIntoLocal, policy)
            .Where(c => c.Status != ItemMergeStatus.NoChange)
            .Distinct(MergeChangeEqualityComparer.Instance);
    }

    private static IEnumerable<MergeChange> CompareImpl(ChangeCollection localChanges,
                                                        ChangeCollection toBeMergedIntoLocal,
                                                        ComparisonPolicy policy)
    {
        foreach (var their in toBeMergedIntoLocal.OrderBy(c => c.Path.FolderPath.Length))
        {
            var oldPath = their.Old?.Path;
            var newPath = their.New?.Path;
            yield return new(policy,
                ancestor: their.Old,
                ours: localChanges.FirstOrDefault(c =>
                    (their.Old?.Path!.Equals(c.Old?.Path) ?? false) ||
                    (their.New?.Path!.Equals(c.New?.Path) ?? false))?.New ?? their.Old,
                theirs: their.New,
                ourRootDeletedParent: SearchForRootDeletion(localChanges.Deleted, oldPath, newPath),
                theirRootDeletedParent: SearchForRootDeletion(toBeMergedIntoLocal.Deleted, oldPath, newPath));

            // Search for local edits/adds on parents that have been removed from theirs
            if (their.Status == ChangeStatus.Delete)
            {
                foreach (var local in localChanges.Added
                                                  .Concat(localChanges.Modified)
                                                  .Concat(localChanges.Renamed)
                                                  .Where(c => IsNestedChildOf(c.New!.Path!, their.Old!.Path!)))
                {
                    yield return new(policy,
                                     ancestor: local.Old,
                                     ours: local.New,
                                     theirRootDeletedParent: their.Old);
                }
            }
        }
    }

    private static TreeItem? SearchForRootDeletion(IEnumerable<Change> deletions, DataPath? oldPath, DataPath? newPath) =>
        (from c in deletions
            where IsNestedChildOf(oldPath, c.Old?.Path) || IsNestedChildOf(newPath, c.Old?.Path)
            orderby c.Old?.Path?.FolderPath.Length ascending // We want the closest parent from root
            select c.Old).FirstOrDefault();

    private static bool IsNestedChildOf(DataPath? path, DataPath? parentPath) =>
        path is not null &&
        parentPath is not null &&
        path.FolderPath.StartsWith(parentPath.FolderPath, StringComparison.Ordinal);
}
