using GitObjectDb.Internal.Queries;
using KellermanSoftware.CompareNetObjects;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GitObjectDb.Comparison
{
    internal class Comparer : IComparer, IComparerInternal
    {
        private readonly IQuery<LoadItem.Parameters, ITreeItem> _nodeLoader;

        public Comparer(IQuery<LoadItem.Parameters, ITreeItem> nodeLoader)
        {
            _nodeLoader = nodeLoader;
        }

        internal delegate bool ConflictResolver(PropertyInfo property, object ancestorValue, object ourValue, object theirValue, out object result);

        public ComparisonResult Compare(object? expectedObject, object? actualObject, ComparisonPolicy policy)
        {
            return CompareInternal(expectedObject, actualObject, policy);
        }

        internal static IEnumerable<PropertyInfo> GetProperties(Type type, ComparisonPolicy policy) =>
            type.GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.CanWrite && !policy.IgnoredProperties.Contains(p));

        internal static ComparisonResult CompareInternal(object? expectedObject, object? actualObject, ComparisonPolicy policy)
        {
            var logic = Cache.Get(policy);
            return logic.Compare(expectedObject, actualObject);
        }

        public ChangeCollection Compare(IConnectionInternal connection, Tree oldTree, Tree newTree, ComparisonPolicy policy)
        {
            if (newTree == null)
            {
                newTree = connection.Repository.Head.Tip.Tree;
            }
            using (var changes = connection.Repository.Diff.Compare<Patch>(oldTree, newTree))
            {
                var result = new ChangeCollection();
                foreach (var change in changes)
                {
                    var oldPath = new Lazy<DataPath>(() => DataPath.FromGitBlobPath(change.OldPath));
                    var old = new Lazy<ITreeItem>(() => _nodeLoader.Execute(
                        connection,
                        new LoadItem.Parameters(oldTree, oldPath.Value, null)));
                    var newPath = new Lazy<DataPath>(() => DataPath.FromGitBlobPath(change.Path));
                    var @new = new Lazy<ITreeItem>(() => _nodeLoader.Execute(
                        connection,
                        new LoadItem.Parameters(newTree, newPath.Value, null)));

                    Change? treeChange;
                    switch (change.Status)
                    {
                        case ChangeKind.Modified:
                            treeChange = Change.Create(change, old.Value, @new.Value, ChangeStatus.Edit, policy);
                            break;
                        case ChangeKind.Added:
                            treeChange = Change.Create(change, default, @new.Value, ChangeStatus.Add, policy);
                            break;
                        case ChangeKind.Deleted:
                            treeChange = Change.Create(change, old.Value, default, ChangeStatus.Delete, policy);
                            break;
                        default:
                            throw new NotImplementedException(change.Status.ToString());
                    }
                    if (treeChange != null)
                    {
                        result.Add(treeChange);
                    }
                }
                return result;
            }
        }

        internal static IEnumerable<MergeChange> Compare(ChangeCollection localChanges, ChangeCollection toBeMergedIntoLocal, ComparisonPolicy policy) =>
            CompareImpl(localChanges, toBeMergedIntoLocal, policy).Where(c => c.Status != ItemMergeStatus.NoChange);

        private static IEnumerable<MergeChange> CompareImpl(ChangeCollection localChanges, ChangeCollection toBeMergedIntoLocal, ComparisonPolicy policy)
        {
            foreach (var their in toBeMergedIntoLocal)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                switch (their.Status)
                {
                    case ChangeStatus.Edit:
                        yield return new MergeChange(policy)
                        {
                            Ancestor = their.Old,
                            Ours = localChanges.Modified.FirstOrDefault(c => their.Old.Path.Equals(c.Old.Path))?.New ?? their.Old,
                            Theirs = their.New,
                            OurRootDeletedParent = (from c in localChanges.Deleted
                                                    where their.Old.Path.FolderPath.StartsWith(c.Old.Path.FolderPath, StringComparison.Ordinal)
                                                    orderby c.Old.Path.FolderPath.Length ascending
                                                    select c.Old).FirstOrDefault(),
                        }.Initialize();
                        break;
                    case ChangeStatus.Add:
                        yield return new MergeChange(policy)
                        {
                            Ours = localChanges.Added.FirstOrDefault(c => c.New.Path?.Equals(their.New.Path) ?? false)?.New,
                            Theirs = their.New,
                            OurRootDeletedParent = (from c in localChanges.Deleted
                                                    where their.New.Path.FolderPath.StartsWith(c.Old.Path.FolderPath, StringComparison.Ordinal)
                                                    orderby c.Old.Path.FolderPath.Length ascending
                                                    select c.Old).FirstOrDefault(),
                        }.Initialize();
                        break;
                    case ChangeStatus.Delete:
                        // Just ignore deletion if deleted in our changes too
                        if (localChanges.Deleted.Any(c => their.Old.Path.FolderPath.StartsWith(c.Old.Path.FolderPath, StringComparison.Ordinal)))
                        {
                            continue;
                        }

                        yield return new MergeChange(policy)
                        {
                            Ancestor = their.Old,
                            Ours = localChanges.FirstOrDefault(c => c.Path?.Equals(their.Old.Path) ?? false)?.New ?? their.Old,
                        }.Initialize();

                        bool IsAddedChildInOurChanges(Change change) =>
                            change.New.Path.FolderPath.StartsWith(their.Old.Path.FolderPath, StringComparison.Ordinal);
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
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        internal static class Cache
        {
            private static readonly ConditionalWeakTable<ComparisonPolicy, CompareLogic> _cache = new ConditionalWeakTable<ComparisonPolicy, CompareLogic>();

            internal static CompareLogic Get(ComparisonPolicy policy) =>
                _cache.GetValue(policy, CreateCompareLogic);

            private static CompareLogic CreateCompareLogic(ComparisonPolicy policy)
            {
                var config = new ComparisonConfig
                {
                    MaxDifferences = int.MaxValue,
                    SkipInvalidIndexers = true,
                    MembersToIgnore = policy.IgnoredProperties.Select(p => $"{p.DeclaringType.Name}.{p.Name}").ToList(),
                };
                return new CompareLogic(config);
            }
        }
    }
}
