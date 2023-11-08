using GitObjectDb.Internal.Queries;
using KellermanSoftware.CompareNetObjects;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace GitObjectDb.Comparison;

internal class Comparer : IComparer, IComparerInternal
{
    private readonly IQuery<LoadItem.Parameters, TreeItem?> _nodeLoader;
    private readonly INodeSerializer _serializer;

    public Comparer(IQuery<LoadItem.Parameters, TreeItem?> nodeLoader, INodeSerializer serializer)
    {
        _nodeLoader = nodeLoader;
        _serializer = serializer;
    }

    public ComparisonResult Compare(object? expectedObject, object? actualObject, ComparisonPolicy policy)
    {
        return CompareInternal(expectedObject, actualObject, policy);
    }

    public ChangeCollection Compare(IConnectionInternal connection,
                                    Commit old,
                                    Commit @new,
                                    ComparisonPolicy? policy = null)
    {
        using var changes = connection.Repository.Diff.Compare<Patch>(old.Tree, @new.Tree);
        var avoidDuplicates = new HashSet<string>(StringComparer.Ordinal);
        var result = new ChangeCollection(@new);
        foreach (var change in changes)
        {
            var transformedChange = Compare(connection, old.Tree, @new.Tree, change, avoidDuplicates, policy);
            if (transformedChange is not null)
            {
                result.Add(transformedChange);
            }
        }
        return result;
    }

    public Change? Compare(IQueryAccessor queryAccessor,
                                        Tree oldTree,
                                        Tree newTree,
                                        PatchEntryChanges change,
                                        ISet<string> avoidDuplicates,
                                        ComparisonPolicy? policy)
    {
        if (IsNodePropertyStoredAsFile(change.OldPath, out var oldNodePath) |
            IsNodePropertyStoredAsFile(change.Path, out var newNodePath))
        {
            if (avoidDuplicates.Contains(oldNodePath!))
            {
                return null;
            }

            avoidDuplicates.Add(oldNodePath!);
            avoidDuplicates.Add(newNodePath!);
            return TurnExternalPropertyIntoNodeChange();
        }

        return change.Status switch
        {
            ChangeKind.Modified => CreateChange(Load(oldTree, change.OldPath),
                Load(newTree, change.Path),
                ChangeStatus.Edit),
            ChangeKind.Added => CreateChange(default, Load(newTree, change.Path), ChangeStatus.Add),
            ChangeKind.Deleted => CreateChange(Load(oldTree, change.OldPath), default, ChangeStatus.Delete),
            ChangeKind.Renamed => CreateChange(Load(oldTree, change.OldPath),
                Load(newTree, change.Path),
                ChangeStatus.Rename),
            _ => null,
        };
        Change? CreateChange(TreeItem? old, TreeItem? @new, ChangeStatus status) => Change.Create(
            change, old, @new, status, policy ?? queryAccessor.Model.DefaultComparisonPolicy);
        TreeItem? Load(Tree tree, string path) => _nodeLoader.Execute(
            queryAccessor,
            new(tree, Index: null, DataPath.Parse(path)));

        Change? TurnExternalPropertyIntoNodeChange()
        {
            var oldNode = oldNodePath is not null ? Load(oldTree, oldNodePath) : null;
            var newNode = newNodePath is not null ? Load(newTree, newNodePath) : null;
            var status = change.Status switch
            {
                _ when oldNodePath is null => ChangeStatus.Add,
                _ when newNodePath is null => ChangeStatus.Delete,
                ChangeKind.Renamed => ChangeStatus.Rename,
                _ => ChangeStatus.Edit,
            };
            return CreateChange(oldNode, newNode, status);
        }
    }

    private bool IsNodePropertyStoredAsFile(string path, out string? nodePath)
    {
        var match = Regex.Match(path, @"^(?<folder>.*)/(?<fileName>\w+)\.\w+\.\w+");
        if (match.Success)
        {
            var fileName = $"{match.Result("${fileName}")}.{_serializer.FileExtension}";
            nodePath = $"{match.Result("${folder}")}/{fileName}";
            return true;
        }
        nodePath = null;
        return false;
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
