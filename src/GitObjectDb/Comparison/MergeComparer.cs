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
                             ours: localChanges.FirstOrDefault(c => c.Status != ChangeStatus.Delete && their.Old!.Path!.Equals(c.Old?.Path))?.New ?? their.Old,
                             theirs: their.New,
                             ourRootDeletedParent: (from c in localChanges.Deleted
                                                    where IsNestedChildOf(oldPath, c.Old?.Path) ||
                                                            IsNestedChildOf(newPath, c.Old?.Path) ||
                                                            IsNestedChildOf(oldPath, c.New?.Path) ||
                                                            IsNestedChildOf(newPath, c.New?.Path)
                                                    orderby c.Old?.Path?.FolderPath.Length ascending
                                                    select c.Old).FirstOrDefault(),
                             theirRootDeletedParent: (from c in toBeMergedIntoLocal.Deleted
                                                      where IsNestedChildOf(oldPath, c.Old?.Path) ||
                                                          IsNestedChildOf(newPath, c.Old?.Path) ||
                                                          IsNestedChildOf(oldPath, c.New?.Path) ||
                                                          IsNestedChildOf(newPath, c.New?.Path)
                                                      orderby c.Old?.Path?.FolderPath.Length ascending
                                                      select c.Old).FirstOrDefault());

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

    static bool IsNestedChildOf(DataPath? path, DataPath? parentPath) =>
        path is not null &&
        parentPath is not null &&
        path.FolderPath.StartsWith(parentPath?.FolderPath, StringComparison.Ordinal);
}
