using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Comparison
{
    internal class MergeComparer : IMergeComparer
    {
        public IEnumerable<MergeChange> Compare(ChangeCollection localChanges, ChangeCollection toBeMergedIntoLocal, ComparisonPolicy policy)
        {
            return CompareImpl(localChanges, toBeMergedIntoLocal, policy)
                .Where(c => c.Status != ItemMergeStatus.NoChange)
                .Distinct(MergeChangeEqualityComparer.Instance);
        }

        private IEnumerable<MergeChange> CompareImpl(ChangeCollection localChanges, ChangeCollection toBeMergedIntoLocal, ComparisonPolicy policy)
        {
            foreach (var their in toBeMergedIntoLocal)
            {
                switch (their.Status)
                {
                    case ChangeStatus.Edit:
                        yield return new MergeChange(policy)
                        {
                            Ancestor = their.Old,
                            Ours = localChanges.Modified.FirstOrDefault(c => their.Old!.Path!.Equals(c.Old.Path))?.New ?? their.Old,
                            Theirs = their.New,
                            OurRootDeletedParent = (from c in localChanges.Deleted
                                                    where their.Old!.Path!.FolderPath.StartsWith(c.Old.Path.FolderPath, StringComparison.Ordinal)
                                                    orderby c.Old.Path.FolderPath.Length ascending
                                                    select c.Old).FirstOrDefault(),
                        }.Initialize();
                        break;
                    case ChangeStatus.Add:
                        yield return new MergeChange(policy)
                        {
                            Ours = localChanges.Added.FirstOrDefault(c => c.New.Path?.Equals(their.New!.Path!) ?? false)?.New,
                            Theirs = their.New,
                            OurRootDeletedParent = (from c in localChanges.Deleted
                                                    where their.New!.Path!.FolderPath.StartsWith(c.Old.Path.FolderPath, StringComparison.Ordinal)
                                                    orderby c.Old.Path.FolderPath.Length ascending
                                                    select c.Old).FirstOrDefault(),
                        }.Initialize();
                        break;
                    case ChangeStatus.Delete:
                        // Just ignore deletion if deleted in our changes too
                        if (localChanges.Deleted.Any(c => their.Old!.Path!.FolderPath.StartsWith(c.Old.Path.FolderPath, StringComparison.Ordinal)))
                        {
                            continue;
                        }

                        yield return new MergeChange(policy)
                        {
                            Ancestor = their.Old,
                            Ours = localChanges.FirstOrDefault(c => c.Path?.Equals(their.Old!.Path) ?? false)?.New ?? their.Old,
                        }.Initialize();

                        bool IsAddedChildInOurChanges(Change change) =>
                            change.New.Path.FolderPath.StartsWith(their.Old!.Path!.FolderPath, StringComparison.Ordinal);
                        foreach (var local in localChanges.Added.Where(IsAddedChildInOurChanges))
                        {
                            yield return new MergeChange(policy)
                            {
                                Ancestor = local.Old,
                                Ours = local.New,
                                TheirRootDeletedParent = their.Old,
                            }.Initialize();
                        }
                        break;
                }
            }
        }
    }
}
