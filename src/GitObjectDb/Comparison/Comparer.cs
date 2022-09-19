using GitObjectDb.Internal.Queries;
using KellermanSoftware.CompareNetObjects;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GitObjectDb.Comparison;

internal class Comparer : IComparer, IComparerInternal
{
    private readonly IQuery<LoadItem.Parameters, ITreeItem> _nodeLoader;

    public Comparer(IQuery<LoadItem.Parameters, ITreeItem> nodeLoader)
    {
        _nodeLoader = nodeLoader;
    }

    public ComparisonResult Compare(ITreeItem? expectedObject, ITreeItem? actualObject, ComparisonPolicy policy)
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

    public ChangeCollection Compare(IConnectionInternal connection, Tree oldTree, Tree newTree, ComparisonPolicy? policy = null)
    {
        if (newTree == null)
        {
            newTree = connection.Repository.Head.Tip.Tree;
        }
        using var changes = connection.Repository.Diff.Compare<Patch>(oldTree, newTree);
        var result = new ChangeCollection();
        foreach (var change in changes)
        {
            var oldPath = new Lazy<DataPath>(() => DataPath.Parse(change.OldPath));
            var old = new Lazy<ITreeItem>(() => _nodeLoader.Execute(
                connection,
                new LoadItem.Parameters(oldTree, oldPath.Value, null)));
            var newPath = new Lazy<DataPath>(() => DataPath.Parse(change.Path));
            var @new = new Lazy<ITreeItem>(() => _nodeLoader.Execute(
                connection,
                new LoadItem.Parameters(newTree, newPath.Value, null)));
            var treeChange = change.Status switch
            {
                ChangeKind.Modified => Change.Create(change, old.Value, @new.Value, ChangeStatus.Edit, policy ?? connection.Model.DefaultComparisonPolicy),
                ChangeKind.Added => Change.Create(change, default, @new.Value, ChangeStatus.Add, policy ?? connection.Model.DefaultComparisonPolicy),
                ChangeKind.Deleted => Change.Create(change, old.Value, default, ChangeStatus.Delete, policy ?? connection.Model.DefaultComparisonPolicy),
                _ => throw new NotImplementedException(change.Status.ToString()),
            };
            if (treeChange != null)
            {
                result.Add(treeChange);
            }
        }
        return result;
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
            };
            return new CompareLogic(config);
        }
    }
}
