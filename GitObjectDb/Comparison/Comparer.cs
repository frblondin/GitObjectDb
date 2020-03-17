using GitObjectDb.Queries;
using GitObjectDb.Serialization.Json;
using KellermanSoftware.CompareNetObjects;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace GitObjectDb.Comparison
{
    internal class Comparer
    {
        private readonly IQuery<Tree, DataPath, ITreeItem> _nodeLoader;

        public Comparer(IQuery<Tree, DataPath, ITreeItem> nodeLoader)
        {
            _nodeLoader = nodeLoader ?? throw new ArgumentNullException(nameof(nodeLoader));
        }

        internal delegate bool ConflictResolver(PropertyInfo property, object ancestorValue, object ourValue, object theirValue, out object result);

        internal static IEnumerable<PropertyInfo> GetProperties(Type type, ComparisonPolicy policy) =>
            type.GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.CanWrite && !policy.IgnoredProperties.Contains(p));

        internal static ComparisonResult Compare(object expectedObject, object actualObject, ComparisonPolicy policy)
        {
            if (policy is null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            var logic = Cache.Get(policy);
            return logic.Compare(expectedObject, actualObject);
        }

        internal ChangeCollection Compare(Repository repository, Tree oldTree, Tree newTree, ComparisonPolicy policy)
        {
            if (newTree == null)
            {
                newTree = repository.Head.Tip.Tree;
            }
            using (var changes = repository.Diff.Compare<Patch>(oldTree, newTree))
            {
                var result = new ChangeCollection();
                foreach (var change in changes)
                {
                    var oldPath = new Lazy<DataPath>(() => DataPath.FromGitBlobPath(change.OldPath));
                    var old = new Lazy<ITreeItem>(() => _nodeLoader.Execute(
                        repository, oldTree[oldPath.Value.FolderPath].Target.Peel<Tree>(), oldPath.Value));
                    var newPath = new Lazy<DataPath>(() => DataPath.FromGitBlobPath(change.Path));
                    var @new = new Lazy<ITreeItem>(() => _nodeLoader.Execute(
                        repository, newTree[newPath.Value.FolderPath].Target.Peel<Tree>(), newPath.Value));

                    switch (change.Status)
                    {
                        case ChangeKind.Modified:
                            var treeChange = Change.Create(change, old.Value, @new.Value, ChangeStatus.Edit, policy);
                            var differences = Compare(old.Value, @new.Value, policy);
                            if (treeChange != null)
                            {
                                result.Add(treeChange);
                            }
                            break;
                        case ChangeKind.Added:
                            result.Add(Change.Create(change, default, @new.Value, ChangeStatus.Add, policy));
                            break;
                        case ChangeKind.Deleted:
                            result.Add(Change.Create(change, old.Value, default, ChangeStatus.Delete, policy));
                            break;
                        default:
                            throw new NotImplementedException(change.Status.ToString());
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
                switch (their.Status)
                {
                    case ChangeStatus.Edit:
                        yield return new MergeChange(policy)
                        {
                            Ancestor = their.Old,
                            Ours = localChanges.Modified.FirstOrDefault(c => their.Old.Path.Equals((DataPath)c.Old.Path))?.New,
                            Theirs = their.New,
                            OurRootDeletedParent = (from c in localChanges.Deleted
                                                    where their.Old.Path.FolderPath.StartsWith(c.Old.Path.FolderPath)
                                                    orderby c.Old.Path.FolderPath.Length ascending
                                                    select c.Old).FirstOrDefault(),
                        }.Initialize();
                        break;
                    case ChangeStatus.Add:
                        yield return new MergeChange(policy)
                        {
                            Ours = localChanges.Added.FirstOrDefault(c => c.New.Path == their.New.Path)?.New,
                            Theirs = their.New,
                            OurRootDeletedParent = (from c in localChanges.Deleted
                                                    where their.Old.Path.FolderPath.StartsWith(c.Old.Path.FolderPath)
                                                    orderby c.Old.Path.FolderPath.Length ascending
                                                    select c.Old).FirstOrDefault(),
                        }.Initialize();
                        break;
                    case ChangeStatus.Delete:
                        yield return new MergeChange(policy)
                        {
                            Ancestor = their.Old,
                            TheirRootDeletedParent = (from c in toBeMergedIntoLocal.Deleted
                                                      where c.Old.Path.FolderPath.StartsWith(their.Old.Path.FolderPath)
                                                      orderby their.Old.Path.FolderPath.Length ascending
                                                      select c.Old).FirstOrDefault(),
                        }.Initialize();

                        // Node or child node added in our changes... ?
                        if (localChanges.Added.Any(c => c.New.Path.FilePath.StartsWith(their.Old.Path.FolderPath)))
                        {
                            throw new NotImplementedException();
                        }
                        break;
                }
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
