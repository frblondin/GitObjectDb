using GitObjectDb.Internal.Queries;
using KellermanSoftware.CompareNetObjects;
using LibGit2Sharp;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GitObjectDb.Comparison;

internal class Comparer : IComparer, IComparerInternal
{
    private readonly IQuery<LoadItem.Parameters, TreeItem?> _nodeLoader;

    public Comparer(IQuery<LoadItem.Parameters, TreeItem?> nodeLoader)
    {
        _nodeLoader = nodeLoader;
    }

    public ComparisonResult Compare(TreeItem? expectedObject, TreeItem? actualObject, ComparisonPolicy policy)
    {
        return CompareInternal(expectedObject, actualObject, policy);
    }

    public ChangeCollection Compare(IConnectionInternal connection,
                                    Commit old,
                                    Commit @new,
                                    ComparisonPolicy? policy = null)
    {
        using var changes = connection.Repository.Diff.Compare<Patch>(old.Tree, @new.Tree);
        var result = new ChangeCollection(@new);
        foreach (var change in changes)
        {
            var treeChanges = Compare(connection, old.Tree, @new.Tree, change, policy);
            result.AddRange(treeChanges.Where(c => c is not null)!);
        }
        return result;
    }

    public IEnumerable<Change?> Compare(IQueryAccessor queryAccessor,
                                        Tree oldTree,
                                        Tree newTree,
                                        PatchEntryChanges change,
                                        ComparisonPolicy? policy)
    {
        if (change.Status == ChangeKind.Modified)
        {
            yield return CreateChange(CreateNode(oldTree, change.OldPath),
                                      CreateNode(newTree, change.Path),
                                      ChangeStatus.Edit);
        }
        else if (change.Status == ChangeKind.Added)
        {
            yield return CreateChange(default, CreateNode(newTree, change.Path), ChangeStatus.Add);
        }
        else if (change.Status == ChangeKind.Deleted)
        {
            yield return CreateChange(CreateNode(oldTree, change.OldPath), default, ChangeStatus.Delete);
        }
        else if (change.Status == ChangeKind.Renamed)
        {
            yield return CreateChange(CreateNode(oldTree, change.OldPath),
                                      CreateNode(newTree, change.Path),
                                      ChangeStatus.Rename);
        }
        Change? CreateChange(TreeItem? old, TreeItem? @new, ChangeStatus status) => Change.Create(
            change, old, @new, status, policy ?? queryAccessor.Model.DefaultComparisonPolicy);
        TreeItem? CreateNode(Tree tree, string path) =>
            _nodeLoader.Execute(
                queryAccessor,
                new(tree, Index: null, DataPath.Parse(path)));
    }

    internal static ComparisonResult CompareInternal(object? expectedObject,
                                                     object? actualObject,
                                                     ComparisonPolicy policy)
    {
        var logic = Cache.Get(policy);
        return logic.Compare(expectedObject, actualObject);
    }

    internal static class Cache
    {
        private static readonly ConditionalWeakTable<ComparisonPolicy, CompareLogic> _cache = new();

        internal static CompareLogic Get(ComparisonPolicy policy) =>
            _cache.GetValue(policy, CreateCompareLogic);

        private static CompareLogic CreateCompareLogic(ComparisonPolicy policy)
        {
            var config = new ComparisonConfig
            {
                MaxDifferences = int.MaxValue,
                SkipInvalidIndexers = true,
                MembersToIgnore = policy.IgnoredProperties.Select(p => $"{p.DeclaringType.Name}.{p.Name}").ToList(),
                AttributesToIgnore = policy.AttributesToIgnore.ToList(),
                TreatStringEmptyAndNullTheSame = policy.TreatStringEmptyAndNullTheSame,
                IgnoreStringLeadingTrailingWhitespace = policy.IgnoreStringLeadingTrailingWhitespace,
            };
            config.CustomComparers.AddRange(policy.CustomComparers);
            return new CompareLogic(config);
        }
    }
}
