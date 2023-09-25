using GitObjectDb.Internal.Queries;
using GitObjectDb.Tools;
using LibGit2Sharp;
using ObjectsComparer;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GitObjectDb.Comparison;

internal class Comparer : IComparer, IComparerInternal
{
    private readonly IQuery<LoadItem.Parameters, TreeItem?> _nodeLoader;

    public Comparer(IQuery<LoadItem.Parameters, TreeItem?> nodeLoader)
    {
        _nodeLoader = nodeLoader;
    }

    public bool Compare(object? expectedObject, object? actualObject, ComparisonPolicy policy, out IEnumerable<Difference> result)
    {
        return CompareInternal(expectedObject, actualObject, policy, out result);
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

    internal static bool CompareInternal(object? expectedObject,
                                         object? actualObject,
                                         ComparisonPolicy policy,
                                         out IEnumerable<Difference> result)
    {
        var logic = Cache.Get(policy);
        var type = expectedObject?.GetType() ?? actualObject?.GetType() ?? typeof(object);
        return logic.Compare(type, expectedObject, actualObject, out result);
    }

    internal static class Cache
    {
        private static readonly ConditionalWeakTable<ComparisonPolicy, ObjectsComparer.Comparer> _cache = new();

        internal static ObjectsComparer.Comparer Get(ComparisonPolicy policy) =>
            _cache.GetValue(policy, CreateCompareLogic);

        private static ObjectsComparer.Comparer CreateCompareLogic(ComparisonPolicy policy)
        {
            var comparer = new ObjectsComparer.Comparer(
                new ComparisonSettings
                {
                    EmptyAndNullEnumerablesEqual = true,
                });

            comparer.AddComparerOverride(
                NodeComparerLimitedToPath.Instance,
                member => member is PropertyInfo property &&
                (property.PropertyType.IsNode() || property.PropertyType.IsNodeEnumerable(out var _)) &&
                member.DeclaringType != null);
            comparer.IgnoreMember(member => policy.IgnoredProperties.Contains(member));
            comparer.IgnoreMember(member => policy.AttributesToIgnore.Any(attribute => member.IsDefined(attribute, inherit: true)));
            comparer.AddComparerOverride<string>(
                new CustomStringComparer(policy.TreatStringEmptyAndNullTheSame, policy.IgnoreStringLeadingTrailingWhitespace));

            policy.Configure?.Invoke(comparer);

            return comparer;
        }
    }
}
