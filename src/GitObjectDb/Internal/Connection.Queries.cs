using GitObjectDb.Comparison;
using LibGit2Sharp;
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

    public TItem? Lookup<TItem>(string committish,
                                DataPath path)
        where TItem : TreeItem
    {
        var (commit, _) = TryGetTree(committish, path);
        return commit.Tree[path.FilePath] is null ?
            default :
            (TItem)_loader.Execute(this, new(commit.Tree, Index: null, path))!;
    }

    public TItem? Lookup<TItem>(string committish,
                                UniqueId id)
        where TItem : TreeItem
    {
        var (commit, path) = TryGetTree(committish, id);
        return path is null ?
            default :
            (TItem)_loader.Execute(this, new(commit.Tree, Index: null, path))!;
    }

    public ICommitEnumerable<TItem> GetItems<TItem>(string committish,
                                                    Node? parent = null,
                                                    bool isRecursive = false)
        where TItem : TreeItem
    {
        var (commit, relativeTree) = TryGetTree(committish, parent?.Path);

        if (relativeTree is null)
        {
            return CommitEnumerable.Empty<TItem>(commit.Id);
        }

        return _queryItems
            .Execute(this, new(commit.Tree,
                               relativeTree,
                               Index: null,
                               typeof(TItem),
                               parent?.Path,
                               isRecursive))
            .AsParallel()
                .Select(i => i.Item.Value)
                .OfType<TItem>()
                .OrderBy(i => i.Path)
            .AsSequential()
            .ToCommitEnumerable(commit.Id);
    }

    public ICommitEnumerable<TNode> GetNodes<TNode>(string committish,
                                                    Node? parent = null,
                                                    bool isRecursive = false)
        where TNode : Node
    {
        return GetItems<TNode>(committish, parent, isRecursive);
    }

    public IEnumerable<DataPath> GetPaths(string committish,
                                          DataPath? parentPath = null,
                                          bool isRecursive = false)
    {
        return GetPaths<TreeItem>(committish, parentPath, isRecursive);
    }

    public IEnumerable<DataPath> GetPaths<TItem>(string committish,
                                                 DataPath? parentPath = null,
                                                 bool isRecursive = false)
        where TItem : TreeItem
    {
        var (commit, relativeTree) = TryGetTree(committish, parentPath);

        if (relativeTree is null)
        {
            return Enumerable.Empty<DataPath>();
        }

        return _queryItems.Execute(this,
                                   new(commit.Tree,
                                       relativeTree,
                                       Index: null,
                                       typeof(TItem),
                                       parentPath,
                                       isRecursive)).Select(i => i.Path);
    }

    public IEnumerable<TreeItem> Search(string committish,
                                         string pattern,
                                         DataPath? parentPath = null,
                                         bool ignoreCase = false,
                                         bool recurseSubModules = false)
    {
        var (commit, _) = TryGetTree(committish, parentPath);
        return _searchItems
            .Execute(this, new(this,
                               commit.Tree,
                               pattern,
                               parentPath,
                               committish,
                               ignoreCase,
                               recurseSubModules))
            .Select(i => i.Item);
    }

    public ICommitEnumerable<Resource> GetResources(string committish, Node node)
    {
        if (node.Path is null)
        {
            throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.");
        }

        var (commit, relativeTree) = TryGetTree(committish, node.Path);

        if (relativeTree is null)
        {
            return CommitEnumerable.Empty<Resource>(commit.Id);
        }

        return _queryResources
            .Execute(this, new(commit.Tree, relativeTree, node))
            .AsParallel()
                .Select(i => i.Resource.Value)
                .OrderBy(i => i.Path)
            .AsSequential()
            .ToCommitEnumerable(commit.Id);
    }

    public ChangeCollection Compare(string startCommittish,
                                    string committish,
                                    ComparisonPolicy? policy = null)
    {
        var (old, _) = TryGetTree(committish: startCommittish);
        var (@new, _) = TryGetTree(committish: committish);
        return _comparer.Compare(this, old, @new, policy ?? Model.DefaultComparisonPolicy);
    }

    public IEnumerable<LogEntry> GetCommits(string branch, Node node)
    {
        var filePath = node.ThrowIfNoPath().FilePath;
        var filter = new CommitFilter
        {
            IncludeReachableFrom = branch,
        };
        return Repository.Commits.QueryBy(filePath, filter);
    }

    private (Commit Commit, Tree? RelativePath) TryGetTree(string committish, DataPath? path = null)
    {
        var commit = (Commit)Repository.Lookup(committish) ??
            throw new GitObjectDbInvalidCommitException();
        if (path is null || string.IsNullOrEmpty(path.FolderPath))
        {
            return (commit, commit.Tree);
        }
        else
        {
            var tree = commit.Tree[path.FolderPath];
            return (commit, tree?.Target.Peel<Tree>());
        }
    }

    private (Commit Commit, DataPath? Path) TryGetTree(string committish, UniqueId id)
    {
        var commit = (Commit)Repository.Lookup(committish) ??
            throw new GitObjectDbInvalidCommitException();

        var stack = new Stack<string>();
        var path = Search(commit.Tree, $"{id}.{Serializer.FileExtension}", stack) ?
            DataPath.Parse(string.Join("/", stack.Reverse())) :
            null;
        return (commit, path);

        static bool Search(Tree tree, string blobName, Stack<string> path)
        {
            foreach (var item in tree)
            {
                path.Push(item.Name);
                if (item.TargetType == TreeEntryTargetType.Blob && item.Name == blobName)
                {
                    return true;
                }
                if (item.TargetType == TreeEntryTargetType.Tree &&
                    !FileSystemStorage.IsResourceName(item.Name) &&
                    Search(item.Target.Peel<Tree>(), blobName, path))
                {
                    return true;
                }
                path.Pop();
            }
            return false;
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
