using GitDotNet;
using GitObjectDb.Internal.Queries;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitObjectDb.Comparison;

internal partial class Comparer(IAsyncQuery<LoadItem.Parameters, TreeItem?> nodeLoader, INodeSerializer serializer) : IComparer, IComparerInternal
{
    public ComparisonResult Compare(object? expectedObject, object? actualObject, ComparisonPolicy policy) =>
        CompareInternal(expectedObject, actualObject, policy);

    public async Task<ChangeCollection> CompareAsync(IConnectionInternal connection,
                                                     CommitEntry old,
                                                     CommitEntry @new,
                                                     ComparisonPolicy? policy = null)
    {
        var changes = await connection.Repository.CompareAsync(old, @new).ConfigureAwait(false);
        var avoidDuplicates = new HashSet<string>(StringComparer.Ordinal);
        var result = new ChangeCollection(@new);
        foreach (var change in changes)
        {
            var oldTree = await old.GetRootTreeAsync().ConfigureAwait(false);
            var newTree = await @new.GetRootTreeAsync().ConfigureAwait(false);
            var transformedChange = await CompareAsync(connection, oldTree, newTree, change, avoidDuplicates, policy).ConfigureAwait(false);
            if (transformedChange is not null)
            {
                result.Add(transformedChange);
            }
        }
        return result;
    }

    public async Task<Change?> CompareAsync(IConnection queryAccessor,
                                            TreeEntry oldTree,
                                            TreeEntry newTree,
                                            GitDotNet.Change change,
                                            ISet<string> avoidDuplicates,
                                            ComparisonPolicy? policy)
    {
        if (IsNodePropertyStoredAsFile(change.OldPath, out var oldNodePath) |
            IsNodePropertyStoredAsFile(change.NewPath, out var newNodePath))
        {
            if (avoidDuplicates.Contains(oldNodePath!))
            {
                return null;
            }

            avoidDuplicates.Add(oldNodePath!);
            avoidDuplicates.Add(newNodePath!);
            return await TurnExternalPropertyIntoNodeChangeAsync().ConfigureAwait(false);
        }

        return change.Type switch
        {
            ChangeType.Modified => CreateChange(
                await LoadAsync(oldTree, change.OldPath!).ConfigureAwait(false),
                await LoadAsync(newTree, change.NewPath!).ConfigureAwait(false),
                ChangeStatus.Edit),
            ChangeType.Added => CreateChange(
                default,
                await LoadAsync(newTree, change.NewPath!).ConfigureAwait(false),
                ChangeStatus.Add),
            ChangeType.Removed => CreateChange(
                await LoadAsync(oldTree, change.OldPath!).ConfigureAwait(false),
                default,
                ChangeStatus.Delete),
            ChangeType.Renamed => CreateChange(
                await LoadAsync(oldTree, change.OldPath!).ConfigureAwait(false),
                await LoadAsync(newTree, change.NewPath!).ConfigureAwait(false),
                ChangeStatus.Rename),
            _ => null,
        };
        Change? CreateChange(TreeItem? old, TreeItem? @new, ChangeStatus status) => Change.Create(
            change, old, @new, status, policy ?? queryAccessor.Model.DefaultComparisonPolicy);
        Task<TreeItem?> LoadAsync(TreeEntry tree, GitPath path) => nodeLoader.ExecuteAsync(
            queryAccessor,
            new(tree, Index: null, DataPath.Parse(path.ToString())));

        async Task<Change?> TurnExternalPropertyIntoNodeChangeAsync()
        {
            var oldNode = await LoadAsync(oldTree, (oldNodePath ?? newNodePath)!).ConfigureAwait(false);
            var newNode = await LoadAsync(newTree, (newNodePath ?? oldNodePath)!).ConfigureAwait(false);
            var status = change.Type switch
            {
                _ when oldNode is null => ChangeStatus.Add,
                _ when newNode is null => ChangeStatus.Delete,
                ChangeType.Renamed => ChangeStatus.Rename,
                _ => ChangeStatus.Edit,
            };
            return CreateChange(oldNode, newNode, status);
        }
    }

    private bool IsNodePropertyStoredAsFile(GitPath? path, out string? nodePath)
    {
        if (path is null)
        {
            nodePath = null;
            return false;
        }
        var match = NodePropertyFileRegex().Match(path.ToString());
        if (match.Success)
        {
            var fileName = $"{match.Result("${fileName}")}.{serializer.FileExtension}";
            nodePath = $"{match.Result("${folder}")}/{fileName}";
            return DataPath.TryParse(nodePath, out var parsed) && parsed.IsNode(serializer);
        }
        nodePath = null;
        return false;
    }

    [GeneratedRegex(@"^(?<folder>.*)/(?<fileName>\w+)\.\w+\.\w+")]
    private static partial Regex NodePropertyFileRegex();

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
                MembersToIgnore = policy.IgnoredProperties.Select(p => $"{p.DeclaringType!.Name}.{p.Name}").ToList(),
                AttributesToIgnore = policy.AttributesToIgnore.ToList(),
                TreatStringEmptyAndNullTheSame = policy.TreatStringEmptyAndNullTheSame,
                IgnoreStringLeadingTrailingWhitespace = policy.IgnoreStringLeadingTrailingWhitespace,
            };
            config.CustomComparers.AddRange(policy.CustomComparers);
            return new CompareLogic(config);
        }
    }
}
