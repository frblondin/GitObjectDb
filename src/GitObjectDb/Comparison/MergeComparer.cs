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
        return MergeComparer.CompareImpl(localChanges, toBeMergedIntoLocal, policy)
            .Where(c => c.Status != ItemMergeStatus.NoChange)
            .Distinct(MergeChangeEqualityComparer.Instance);
    }

    private static IEnumerable<MergeChange> CompareImpl(ChangeCollection localChanges,
                                                        ChangeCollection toBeMergedIntoLocal,
                                                        ComparisonPolicy policy)
    {
        foreach (var their in toBeMergedIntoLocal)
        {
            switch (their.Status)
            {
                case ChangeStatus.Edit:
                    yield return CreateEdit(localChanges, policy, their);
                    break;
                case ChangeStatus.Add:
                    yield return CreateAddition(localChanges, policy, their);
                    break;
                case ChangeStatus.Delete:
                    // Just ignore deletion if deleted in our changes too
                    if (localChanges.Deleted.Any(c => IsNestedChildOf(their.Old!.Path!, c.Old!.Path!)))
                    {
                        continue;
                    }

                    yield return CreateDeletion(localChanges, policy, their);

                    foreach (var local in localChanges.Added.Where(c => IsNestedChildOf(c.New!.Path!, their.Old!.Path!)))
                    {
                        yield return CreateTreeConflict(policy, their, local);
                    }
                    break;
                default:
                    throw new NotSupportedException(nameof(their.Status));
            }
        }
    }

    private static MergeChange CreateEdit(ChangeCollection localChanges,
                                          ComparisonPolicy policy,
                                          Change their) => new MergeChange(policy)
    {
        Ancestor = their.Old,
        Ours = localChanges.Modified.FirstOrDefault(c => their.Old!.Path!.Equals(c.Old?.Path))?.New ?? their.Old,
        Theirs = their.New,
        OurRootDeletedParent = (from c in localChanges.Deleted
                                where IsNestedChildOf(their.Old!.Path!, c.Old?.Path)
                                orderby c.Old?.Path?.FolderPath.Length ascending
                                select c.Old).FirstOrDefault(),
    }.Initialize();

    private static MergeChange CreateAddition(ChangeCollection localChanges,
                                              ComparisonPolicy policy,
                                              Change their) => new MergeChange(policy)
    {
        Ours = localChanges.Added.FirstOrDefault(c => c.New!.Path?.Equals(their.New!.Path!) ?? false)?.New,
        Theirs = their.New,
        OurRootDeletedParent = (from c in localChanges.Deleted
                                where IsNestedChildOf(their.New!.Path!, c.Old?.Path)
                                orderby c.Old?.Path?.FolderPath.Length ascending
                                select c.Old).FirstOrDefault(),
    }.Initialize();

    private static MergeChange CreateDeletion(ChangeCollection localChanges,
                                              ComparisonPolicy policy,
                                              Change their) => new MergeChange(policy)
    {
        Ancestor = their.Old,
        Ours = localChanges.FirstOrDefault(c => c.Path?.Equals(their.Old!.Path) ?? false)?.New ?? their.Old,
    }.Initialize();

    private static MergeChange CreateTreeConflict(ComparisonPolicy policy,
                                                  Change their,
                                                  Change local) => new MergeChange(policy)
    {
        Ancestor = local.Old,
        Ours = local.New,
        TheirRootDeletedParent = their.Old,
    }.Initialize();

    static bool IsNestedChildOf(DataPath path, DataPath? parentPath) =>
        path.FolderPath.StartsWith(parentPath?.FolderPath, StringComparison.Ordinal);
}
