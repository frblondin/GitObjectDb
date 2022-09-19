using GitObjectDb.Comparison;
using GitObjectDb.Internal.Queries;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitObjectDb.Internal;

internal sealed partial class Connection
{
    // Use lazy for concurrent dictionary thread safety
    private readonly ConcurrentDictionary<DataPath, Lazy<Repository>> _repositories = new();

    public TItem Lookup<TItem>(DataPath path,
                               string? committish = null,
                               IMemoryCache? referenceCache = null)
        where TItem : ITreeItem
    {
        var (tree, _) = GetTree(path, committish);
        return (TItem)_loader.Execute(this,
                                      new LoadItem.Parameters(tree,
                                                              path,
                                                              referenceCache ?? CreateEphemeralCache()));
    }

    public IEnumerable<TItem> GetItems<TItem>(Node? parent = null,
                                              string? committish = null,
                                              bool isRecursive = false,
                                              IMemoryCache? referenceCache = null)
        where TItem : ITreeItem
    {
        var (tree, relativeTree) = GetTree(parent?.Path, committish);
        return _queryItems
            .Execute(this, new QueryItems.Parameters(tree,
                                                     relativeTree,
                                                     typeof(TItem),
                                                     parent?.Path,
                                                     isRecursive,
                                                     referenceCache ?? CreateEphemeralCache()))
            .AsParallel()
                .Select(i => i.Item.Value)
                .OfType<TItem>()
                .OrderBy(i => i.Path)
            .AsSequential();
    }

    public IEnumerable<TNode> GetNodes<TNode>(Node? parent = null,
                                              string? committish = null,
                                              bool isRecursive = false,
                                              IMemoryCache? referenceCache = null)
        where TNode : Node
    {
        return GetItems<TNode>(parent, committish, isRecursive, referenceCache);
    }

    public IEnumerable<DataPath> GetPaths(DataPath? parentPath = null,
                                          string? committish = null,
                                          bool isRecursive = false)
    {
        return GetPaths<ITreeItem>(parentPath, committish, isRecursive);
    }

    public IEnumerable<DataPath> GetPaths<TItem>(DataPath? parentPath = null,
                                                 string? committish = null,
                                                 bool isRecursive = false)
        where TItem : ITreeItem
    {
        var (tree, relativeTree) = GetTree(parentPath, committish);
        return _queryItems.Execute(this,
                                   new QueryItems.Parameters(tree,
                                                             relativeTree,
                                                             typeof(TItem),
                                                             parentPath,
                                                             isRecursive,
                                                             CreateEphemeralCache())).Select(i => i.Path);
    }

    public IEnumerable<Resource> GetResources(Node node,
                                              string? committish = null,
                                              IMemoryCache? referenceCache = null)
    {
        if (node.Path is null)
        {
            throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.");
        }

        var (tree, relativeTree) = GetTree(node.Path, committish);
        return _queryResources
            .Execute(this, new QueryResources.Parameters(tree,
                                                         relativeTree,
                                                         node,
                                                         referenceCache ?? CreateEphemeralCache()))
            .AsParallel()
                .Select(i => i.Resource.Value)
                .OrderBy(i => i.Path)
            .AsSequential();
    }

    internal static IMemoryCache CreateEphemeralCache() =>
        new MemoryCache(Options.Create<MemoryCacheOptions>(new()));

    public ChangeCollection Compare(string startCommittish,
                                    string? committish = null,
                                    ComparisonPolicy? policy = null)
    {
        var (old, _) = GetTree(committish: startCommittish);
        var (@new, _) = GetTree(committish: committish);
        return _comparer.Compare(this, old, @new, policy ?? Model.DefaultComparisonPolicy);
    }

    private (Tree Tree, Tree RelativePath) GetTree(DataPath? path = null, string? committish = null)
    {
        var commit = committish != null ?
            (Commit)Repository.Lookup(committish) :
            Repository.Head.Tip;
        if (commit == null)
        {
            throw new GitObjectDbException("No valid commit could be found.");
        }
        if (path is null || string.IsNullOrEmpty(path.FolderPath))
        {
            return (commit.Tree, commit.Tree);
        }
        else
        {
            var tree = commit.Tree[path.FolderPath] ??
                       throw new GitObjectDbException("Requested path could not be found.");
            return (commit.Tree, tree.Target.Peel<Tree>());
        }
    }

    Repository ISubmoduleProvider.GetOrCreateSubmoduleRepository(DataPath path,
                                                                 string url)
    {
        var folderPath = Path.Combine(Repository.Info.Path,
                                      "modules",
                                      path.FolderPath,
                                      FileSystemStorage.ResourceFolder);
        return _repositories.GetOrAdd(path,
                                      new Lazy<Repository>(CreateOrLoad)).Value;
        Repository CreateOrLoad() =>
            LibGit2Sharp.Repository.IsValid(folderPath) ? Load() : Create();

        Repository Load() =>
            new(folderPath);

        Repository Create()
        {
            Directory.CreateDirectory(folderPath);
            LibGit2Sharp.Repository.Clone(url, folderPath, new()
            {
                IsBare = true,
            });
            return Load();
        }
    }
}
